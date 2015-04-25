using UnityEngine;
using System.Collections;

public class LabelBehaviour : MonoBehaviour
{
    
    private _ArrowGramsBuilder arrow_grams_builder;
    public Point attached_point;
    Vector3 mouse_position;
    
    void Awake () {
        arrow_grams_builder = GameObject.Find("Background").GetComponent("_ArrowGramsBuilder") as _ArrowGramsBuilder;
        attached_point = arrow_grams_builder.last_point;
    }
    
    void OnMouseDrag(){
        mouse_position = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
        attached_point.label_offsets = mouse_position - attached_point.container.transform.position;
        attached_point.UpdateLabelPosition();
    }

    void OnMouseDown(){

    }

    void OnMouseUp(){
        arrow_grams_builder.active_midpoint = attached_point;
    }
}

