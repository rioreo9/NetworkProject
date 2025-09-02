using System;
using UnityEngine;

public interface ShipInterFace
{

}

public interface IShipWaypointHandler
{
    public void SetNextWaypoint(Transform nextWaypoint);
}
