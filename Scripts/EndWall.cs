using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndWall : MonoBehaviour
{
    SingleBanManager banManager;

    void Start()
    {
        banManager = SingleBanManager.instance;
    }


    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 10 || other.gameObject.layer == 11)
        {
            CarInfo carInfo = other.GetComponent<CarInfo>();
            if (carInfo != null)
            {
                // Pass the CAR'S LANE INDEX, not the signNumber!
                if (banManager.IsThisCarBanned(carInfo.laneIndex, carInfo))
                {
                    banManager.TakeDamage(1);
                }
                SingleBanManager.instance.OnCarReachedEnd(other.gameObject, carInfo.laneIndex);
            }
            else
            {
                print("car info not attached to game obj");
            }
        }
    }

}
