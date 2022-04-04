using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocationsDisplay : MonoBehaviour
{
    public Text conn_status;
    public Text lati_txt;
    public Text long_txt;
    public Text alti_txt;
    public Text time_disp;

    public float accel_txt_update_time;
    float cur_accel_txt_update_time;
    public Text displ_txt;
    public Text accel_txt;
    public Text bearing_txt;
    public Text time_disp_2;

    void Update()
    {
        System.DateTime t = System.DateTime.Now;

        switch (Locations.conn_status)
        {
            case Locations.gps_status.Disabled: conn_status.text = "Disabled"; break;
            case Locations.gps_status.Unavailable: conn_status.text = "No GPS Chip"; break;
            case Locations.gps_status.Connecting:
                conn_status.text = string.Format("Connecting... (Tries: {0}/{1})",
                    Locations.main.gps_conn_cur_wait, Locations.main.gps_conn_max_wait);
                break;
            case Locations.gps_status.TimeOut: conn_status.text = "Time Out"; break;
            case Locations.gps_status.ConnectUnable: conn_status.text = "Failed"; break;
            case Locations.gps_status.Connected: conn_status.text = "Connected";; break;
        }

        if (Locations.main.loc_service_on)
        {
            //last_updated_time != Input.location.lastData.timestamp
            lati_txt.text = string.Format("Latitude: {0}", Input.location.lastData.latitude);
            long_txt.text = string.Format("Longitude: {0}", Input.location.lastData.longitude);
            alti_txt.text = string.Format("Altitude: {0}", Input.location.lastData.altitude);
            time_disp.text = string.Format("{0}:{1}:{2}",t.Hour,t.Minute,t.Second);

            bearing_txt.text = string.Format("Bearing: {0}", Locations.main.bearing_current);
            
        } else if (Locations.main.use_fake_gps) {
            Vector2 fake_gps = GPSEncoder.USCToGPS(Map.main.user_pos.position);
            lati_txt.text = string.Format("Latitude: {0}", fake_gps.x);
            long_txt.text = string.Format("Longitude: {0}", fake_gps.y);
            alti_txt.text = string.Format("Altitude: {0}", "N/A");

            bearing_txt.text = string.Format("Bearing: {0}", Locations.main.bearing_current);
        }

        cur_accel_txt_update_time += Time.deltaTime;
        if (cur_accel_txt_update_time >= accel_txt_update_time)
        {
            cur_accel_txt_update_time = 0;
            if (Locations.main.loc_service_on)
            {
                displ_txt.text = string.Format("Displacement:\nx:{0:0.0####}, \ny:{1:0.0####}, \nz:{2:0.0####}", 
                    Locations.main.displ.x, Locations.main.displ.y, Locations.main.displ.z);
                time_disp_2.text = string.Format("{0}:{1}:{2}",t.Hour,t.Minute,t.Second);
            } else if (Locations.main.use_fake_gps) {
                displ_txt.text = string.Format("Displacement:\nx:{0:0.0####}, \ny:{1:0.0####}, \nz:{2:0.0####}",
                    Map.main.user_pos.position.x, Map.main.user_pos.position.y, Map.main.user_pos.position.z);
            }
        }
    }
}
