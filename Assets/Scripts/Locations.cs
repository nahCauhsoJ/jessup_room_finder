using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Locations : MonoBehaviour
{
    public static Locations main;

    public Text conn_status;
    public Text lati_txt;
    public Text long_txt;
    public Text alti_txt;
    public Text time_disp;

    public float accel_txt_update_time;
    public Text displ_txt;
    public Text accel_txt;
    public Text bearing_txt;
    public Text time_disp_2;

    public float bearing_max_vel;
    float bearing_smooth_vel;
    public float bearing_current{get; private set;}
    public float bearing_fake_rot; // For debugging use. Use left and right arrow to change it slowly.

    public Vector3 jessup_gps_location; // For altitude, just choose a point and that's our ground level.

    public bool loc_service_on{get; private set;}
    double last_updated_time;
    public Vector3 displ{get; private set;} = Vector3.zero;
    Vector3 accel = Vector3.zero;
    float cur_accel_txt_update_time;

    // Just to test on laptop.
    // Note that fake gps will be used with jessup_gps_location as origin. Hence 0,0 isn't rlly 0,0 in gps reading.
    public Vector3 fake_gps; // x: lati, y: alti, z: long
    bool use_fake_gps;

    void Awake()
    {
        main = this;
        GPSEncoder.SetLocalOrigin(new Vector2 (jessup_gps_location.x, jessup_gps_location.z));
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(StartLocationService());
        //Input.gyro.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        System.DateTime t = System.DateTime.Now;

        if (loc_service_on && last_updated_time != Input.location.lastData.timestamp)
        {
            last_updated_time = Input.location.lastData.timestamp;
            lati_txt.text = string.Format("Latitude: {0}", Input.location.lastData.latitude);
            long_txt.text = string.Format("Longitude: {0}", Input.location.lastData.longitude);
            alti_txt.text = string.Format("Altitude: {0}", Input.location.lastData.altitude);
            time_disp.text = string.Format("{0}:{1}:{2}",t.Hour,t.Minute,t.Second);
        }

        if (loc_service_on)
        {
            bearing_current = Mathf.SmoothDampAngle(bearing_current, Input.compass.trueHeading, ref bearing_smooth_vel, bearing_max_vel);
            bearing_txt.text = string.Format("Bearing: {0}", bearing_current);

            cur_accel_txt_update_time += Time.deltaTime;
            if (cur_accel_txt_update_time >= accel_txt_update_time)
            {
                cur_accel_txt_update_time = 0;
                displ = GPSEncoder.GPSToUCS(Input.location.lastData.latitude, Input.location.lastData.longitude) - 
                        GPSEncoder.GPSToUCS(jessup_gps_location.x, jessup_gps_location.z);
                displ_txt.text = string.Format("Displacement:\nx:{0:0.0####}, \ny:{1:0.0####}, \nz:{2:0.0####}", displ.x, displ.y, displ.z);
                time_disp_2.text = string.Format("{0}:{1}:{2}",t.Hour,t.Minute,t.Second);
            }
        } else if (use_fake_gps) {
            lati_txt.text = string.Format("Latitude: {0}", jessup_gps_location.x + fake_gps.x);
            long_txt.text = string.Format("Longitude: {0}", jessup_gps_location.z + fake_gps.z);
            alti_txt.text = string.Format("Altitude: {0}", jessup_gps_location.y + fake_gps.y);
            displ = GPSEncoder.GPSToUCS(jessup_gps_location.x + fake_gps.x, jessup_gps_location.z + fake_gps.z) - 
                    GPSEncoder.GPSToUCS(jessup_gps_location.x, jessup_gps_location.z);
            displ_txt.text = string.Format("Displacement:\nx:{0:0.0####}, \ny:{1:0.0####}, \nz:{2:0.0####}", displ.x, displ.y, displ.z);

            if (Input.GetKey("left")) bearing_fake_rot += 0.2f;
            if (Input.GetKey("right")) bearing_fake_rot -= 0.2f;
            bearing_current = Mathf.SmoothDampAngle(bearing_current, bearing_fake_rot, ref bearing_smooth_vel, bearing_max_vel);
            bearing_txt.text = string.Format("Bearing: {0}", bearing_current);
        }

        /*
        accel = Input.gyro.userAcceleration;
        displ += accel * 0.5f * Time.deltaTime * Time.deltaTime;

        cur_accel_txt_update_time += Time.deltaTime;
        if (cur_accel_txt_update_time >= accel_txt_update_time)
        {
            cur_accel_txt_update_time = 0;
            accel_txt.text = string.Format("Accel: x:{0:0.0####},\n y:{1:0.0####}, \nz:{2:0.0####}", accel.x, accel.y, accel.z);
            displ_txt.text = string.Format("Disp: x:{0:0.0####}, \ny:{1:0.0####}, \nz:{2:0.0####}", displ.x, displ.y, displ.z);
            time_disp_2.text = string.Format("{0}:{1}:{2}",t.Hour,t.Minute,t.Second);
        }*/
    }

    IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            conn_status.text = "No GPS Chip";
            use_fake_gps = true;
            yield break;
        }

        Input.location.Start(1f,1f);

        conn_status.text = "Connecting...";
        int maxWait = 10;
        int curWait = maxWait;
        while (Input.location.status == LocationServiceStatus.Initializing && curWait > 0)
        {
            yield return new WaitForSeconds(1);
            conn_status.text = string.Format("Connecting... (Tries: {0}/{1})",curWait,maxWait);
            curWait--;
        }

        if (curWait < 1)
        {
            conn_status.text = "Time Out";
            yield break;
        }

        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            conn_status.text = "Failed";
            yield break;
        }
        else
        {
             conn_status.text = "Connected";
            loc_service_on = true;
            Input.compass.enabled = true;
        }
    }
}
