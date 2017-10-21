//
//  KinectBody.cs
//
//  Created by Gavin_KG on 2017/10/17.
//  Copyright © 2017 Gavin_KG. All rights reserved.
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

/// <summary>
/// A wrapper class representing a Kinect generated body.
/// </summary>
public class KinectBody {

    public KinectBody(Body body) {
        kinectBody = body;
    }

    public Body kinectBody = null;
    
    public ulong uuid { get { return kinectBody.TrackingId; } }

}
