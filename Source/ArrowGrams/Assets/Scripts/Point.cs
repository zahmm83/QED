


using UnityEngine;

public class Point {

    public MonoBehaviour parent { get; set;}
    public GameObject container { get; set;}
    public GameObject label { get; set;}
    public Vector3 label_offsets { get; set; }
    public string old_value { get; set; }
    public string arrowgram_value { get; set; }
    public bool use_old_value { get; set; }

    public Point(MonoBehaviour mono, GameObject game_object){
        parent = mono;
        container = game_object.gameObject;
    }

    public void UpdatePosition(Vector3 mouse_position){
        container.transform.position = mouse_position;
        if(label != null){
            UpdateLabelPosition();
        }
    }

    public void UpdateLabelPosition(){
        label.transform.position = container.transform.position + label_offsets;
    }
}
