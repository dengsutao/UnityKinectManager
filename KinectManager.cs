//
//  KinectManager.cs
//
//  Created by Gavin_KG on 2017/10/17.
//  Copyright © 2017 Gavin_KG. All rights reserved.
//

using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEngine;

/// <summary>
/// Kinect SDK wrapper. Should be attached to an empty GameObject. Using events to notify when body enters or exits.
/// </summary>
public sealed class KinectManager : MonoBehaviour {

    public float statusRefreshRate = 1f;

    public static KinectManager instance = null;
    public bool isAvailable { get { return _available; } }
    public string kinectUUID { get { return _sensor.UniqueKinectId; } }
    public int currentBodyCount { get { return _currentBodyCount; } }

    public delegate void BodyEnterDelegate(KinectBody kinectBody);
    public delegate void BodyExitDelegate(KinectBody kinectBody);

    public event BodyEnterDelegate OnBodyEnter;
    public event BodyExitDelegate OnBodyExit;

    private KinectSensor _sensor;
    private BodyFrameReader _bodyFrameReader;
    private Body[] _bodies = null; // Body register for SDK use
    private ulong[] _bodyUUIDs = null; // stores last body uuid in order to track changes
    private KinectBody[] _kinectBodies = null; // stores own class
    private bool _available = false;
    private int _maxBodyCount = 0;
    private int _currentBodyCount = 0;


    void Awake() {

        // Singleton setup.
        if (instance == null) {
            instance = this;
        } else if (instance == this) {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        OnscreenStatsManager.instance.OnUpdateOnscreenStats += OnRenderOnscreenStats;

        _sensor = KinectSensor.GetDefault();

        StartCoroutine(RefreshKinectStatus());
    }

    void Update() {
        if (_available) {
            if (_bodyFrameReader != null) {

                BodyFrame frame = _bodyFrameReader.AcquireLatestFrame();
                if (frame != null) {
                    frame.GetAndRefreshBodyData(_bodies);

                    RefreshBodyArrays();

                    frame.Dispose();
                    frame = null;
                }
            }
        }
    }

    void OnApplicationQuit() {
        if (_bodyFrameReader != null) {
            _bodyFrameReader.IsPaused = true;
            _bodyFrameReader.Dispose();
            _bodyFrameReader = null;
        }

        if (_sensor != null) {
            if (_sensor.IsOpen) {
                _sensor.Close();
            }
            _sensor = null;
        }
    }

    IEnumerator RefreshKinectStatus() {
        while (true) {

            if (_sensor != null) {
                _available = _sensor.IsAvailable;
                _bodyFrameReader = _sensor.BodyFrameSource.OpenReader();
                if (!_sensor.IsOpen) {
                    _sensor.Open();
                }
                _maxBodyCount = _sensor.BodyFrameSource.BodyCount;
                _bodies = new Body[_maxBodyCount]; // Kinect v2 can recognize up to 6 bodies.
                _kinectBodies = new KinectBody[_maxBodyCount]; // stores own class for KinectBody.
                _bodyUUIDs = new ulong[_maxBodyCount];
                for (int i = 0; i < _maxBodyCount; i++) {
                    _bodyUUIDs[i] = 0;
                }
                break;
            } else {
                _available = false;
                yield return new WaitForSeconds(statusRefreshRate);
            }
        }
        yield return null;
    }

    private void RefreshBodyArrays() {
        for (int i = 0; i < _maxBodyCount; i++) {
            if (_bodies[i].TrackingId != _bodyUUIDs[i]) {
                if (_bodies[i].TrackingId == 0) {

                    // Delete
                    if (OnBodyExit != null) {
                        OnBodyExit(_kinectBodies[i]);
                    }
                    
                    _kinectBodies[i] = null;
                    _currentBodyCount--;

                } else {

                    // New body
					print("Detected.");
                    KinectBody kinectBody = new KinectBody(_bodies[i]);
                    if (OnBodyEnter != null) {
                        OnBodyEnter(kinectBody);
                    }
                    
                    _kinectBodies[i] = kinectBody;
                    _currentBodyCount++;
                }
            }
            _bodyUUIDs[i] = _bodies[i].TrackingId;
        }
    }

    public void OnRenderOnscreenStats() {
        string s = "--- Kinect Manager ---\n"
                 + "Device: " + (_available ? _sensor.UniqueKinectId : "Not available") + "\n"
                 + "Current Body Count: " + _currentBodyCount.ToString() + "/" + _maxBodyCount.ToString();
        OnscreenStatsManager.instance.AddStats(s);
    }

    public KinectBody GetFirstBody() {
        foreach (KinectBody  b in _kinectBodies) {
            if (b != null) {
                print(b.kinectBody.TrackingId);
                return b;
            }
        }
        return null;
    }

}
