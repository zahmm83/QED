

// TODO This file should get refactored and split into the various point types to define behaviours separately.
// VertexBehaviour
// MidpointBehaviour


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PointBehaviour : MonoBehaviour {

    private _ArrowGramsBuilder arrow_grams_builder;
    Vector3 mouse_position;
    public Point this_point;
    public int drag_duration = 0;
    public bool this_point_is_active = false;


	void Awake () {
        arrow_grams_builder = GameObject.Find("Background").GetComponent("_ArrowGramsBuilder") as _ArrowGramsBuilder;
        this_point = new Point(this, this.gameObject);
        arrow_grams_builder.points.Add(this_point);
	}

    void OnMouseDrag(){

        // Don't do anything if the user is currently changing a value.
        if(arrow_grams_builder.accepting_input){
            return;
        }
        drag_duration++;

        if(drag_duration > 10){
            Arrow midpoint_arrow = null;
            foreach(Arrow a in arrow_grams_builder.arrows){
                if(a.midpoint == this_point){
                    midpoint_arrow = a;
                }
            }
    
            mouse_position = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
            Vector3 displacement = mouse_position - this_point.container.gameObject.transform.position;

            if(midpoint_arrow == null){
                this_point.UpdatePosition(mouse_position);
            } else {
                // Figure out the closest point on the perpendicular bisector of the arrow to the mouse_position. Thats where the midpoint should be.
                float head_x = midpoint_arrow.head.vertex.container.transform.position.x;
                float head_y = midpoint_arrow.head.vertex.container.transform.position.y;
                float tail_x = midpoint_arrow.tail.vertex.container.transform.position.x;
                float tail_y = midpoint_arrow.tail.vertex.container.transform.position.y;
                float mid_x = midpoint_arrow.midpoint.container.transform.position.x;
                float mid_y = midpoint_arrow.midpoint.container.transform.position.y;

                float slope = (head_y - tail_y)/(head_x - tail_x);
                float perp_slope = -1/slope;
                float a = perp_slope;
                float b = -1.0f;
                float c = mid_y - perp_slope * mid_x;


                Vector3 true_midpoint = new Vector3((head_x + tail_x)/2, (head_y + tail_y)/2, 0);

                float closest_x = 0;
                float closest_y  = 0;
                float closest_displacement = 0;

                // Need to handle the case of horizontal lines, which makes a and c infinity.
                // In this case we just use the y values directly from the mouse.
                if(float.IsInfinity(a) || float.IsInfinity(c)){
                    closest_x = true_midpoint.x;
                    closest_y = Input.mousePosition.y/Screen.height;
                    closest_displacement = Mathf.Abs(closest_y - true_midpoint.y);
                } else {
                    closest_x = (b * (b * mouse_position.x - a * mouse_position.y) - a * c) / (Mathf.Pow(a, 2) + Mathf.Pow(b, 2));
                    closest_y = (a * (-b * mouse_position.x + a * mouse_position.y) - b * c) / (Mathf.Pow(a, 2) + Mathf.Pow(b, 2));
                    // Using the closest distance formula which can be found at http://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
                    closest_displacement = Mathf.Sqrt(Mathf.Pow(mid_x - closest_x ,2) + Mathf.Pow(mid_y - closest_y ,2));
                }

                Debug.Log(closest_displacement);
                float temp_displacement = Mathf.Sqrt(Mathf.Pow(true_midpoint.x - closest_x ,2) + Mathf.Pow(true_midpoint.y - closest_y ,2));
                if(midpoint_arrow.head == midpoint_arrow.tail){
                    // Resize the circle
                    temp_displacement = Mathf.Sqrt(Mathf.Pow(true_midpoint.x - mouse_position.x ,2) + Mathf.Pow(true_midpoint.y - mouse_position.y ,2));
                    if(temp_displacement > 0.03f){
                        midpoint_arrow.locked_displacement = temp_displacement;
                        this_point.UpdatePosition(mouse_position);
                    }
                } else if(midpoint_arrow.is_curved || closest_displacement > 0.01f){  // The value of 0.01 needs to match the same threshold that is_curved uses.
                    // Resize the curve
                    midpoint_arrow.locked_displacement = temp_displacement;
                    this_point.UpdatePosition(new Vector3(closest_x, closest_y, 1.0f));
                } else {
                    // Set it at the true midpoint
                    midpoint_arrow.locked_displacement = temp_displacement;
                    this_point.UpdatePosition(true_midpoint);
                }
                 
            }

            if(arrow_grams_builder.active_points.Count <= 1){
                // We are only moving one point so move everything attached to this point.
                foreach(Arrow a in arrow_grams_builder.arrows){
                    if(this_point == a.head.vertex || this_point == a.tail.vertex){
                        if(!(a.head == a.tail)){
                            // If it's not a loop arrow (arrow attached to a single point)
                            float head_x = a.head.vertex.container.transform.position.x;
                            float head_y = a.head.vertex.container.transform.position.y;
                            float tail_x = a.tail.vertex.container.transform.position.x;
                            float tail_y = a.tail.vertex.container.transform.position.y;
                            float mid_x = a.midpoint.container.transform.position.x;
                            float mid_y = a.midpoint.container.transform.position.y;
                            float slope = (head_y - tail_y)/(head_x - tail_x);
                            
                            Vector3 midpoint = new Vector3((head_x + tail_x)/2, (head_y + tail_y)/2, 0);

                            if(a.is_curved){
                                Vector3 head_pos = a.head.vertex.container.transform.position;
                                Vector3 tail_pos = a.tail.vertex.container.transform.position;
                                float theta = Mathf.Atan((head_pos - tail_pos).y / (head_pos - tail_pos).x);
                                
                                float line_y = slope * (mid_x - head_x) + head_y;
                                bool flip = (mid_y < line_y && Mathf.Sign(slope) == -1.0f)
                                            || (mid_y < line_y && Mathf.Sign(slope) == 1.0f) 
                                            || (head_y == tail_y && mid_y < head_y);
                                
                                float flip_curve = flip ? -1.0f : 1.0f;
                                
                                float new_x = midpoint.x + flip_curve * a.locked_displacement * -Mathf.Sin(theta);
                                float new_y = midpoint.y + flip_curve * a.locked_displacement * Mathf.Cos(theta);
                                midpoint = new Vector3(new_x, new_y, 0);
                            }

                            a.midpoint.UpdatePosition(midpoint);
                        } else {
                            // Move the loop midpoint along with the dragged point.
                            a.midpoint.container.transform.position += displacement;
                        }
                    }
                }
            } else {
                // More than one point is selected, so we'll move them all
                HashSet<Point> points_to_move = new HashSet<Point>();
                foreach(Arrow b in arrow_grams_builder.arrows){
                    bool head_is_active = false;
                    bool tail_is_active = false;
                    for(int i = 0; i < arrow_grams_builder.active_points.Count; i++){
                        if(arrow_grams_builder.active_points[i] == b.head.vertex){
                            head_is_active = true;
                        }
                        if(arrow_grams_builder.active_points[i] == b.tail.vertex){
                            tail_is_active = true;
                        }
                        if(b.head.vertex != this_point && head_is_active){
                            points_to_move.Add(b.head.vertex);
                        }
                        if(b.tail.vertex != this_point && tail_is_active){
                            points_to_move.Add(b.tail.vertex);
                        }
                        if(head_is_active && tail_is_active){
                            // If its a midpoint of a fully selected arrow we move it with the arrow
                            points_to_move.Add(b.midpoint);
                        } else if (head_is_active || tail_is_active){
                            // If its a midpoint of a partially selected arrow then update is based on if the arrow is curved or not
                            if(!(b.head == b.tail)){
                                // If it not a loop arrow
                                float head_x = b.head.vertex.container.transform.position.x;
                                float head_y = b.head.vertex.container.transform.position.y;
                                float tail_x = b.tail.vertex.container.transform.position.x;
                                float tail_y = b.tail.vertex.container.transform.position.y;
                                float mid_x = b.midpoint.container.transform.position.x;
                                float mid_y = b.midpoint.container.transform.position.y;
                                float slope = (head_y - tail_y)/(head_x - tail_x);
                                
                                Vector3 midpoint = new Vector3((head_x + tail_x)/2, (head_y + tail_y)/2, 0);

                                if(b.is_curved){
                                    Vector3 head_pos = b.head.vertex.container.transform.position;
                                    Vector3 tail_pos = b.tail.vertex.container.transform.position;
                                    float theta = Mathf.Atan((head_pos - tail_pos).y / (head_pos - tail_pos).x);
                                    
                                    float line_y = slope * (mid_x - head_x) + head_y;
                                    bool flip = (mid_y < line_y && Mathf.Sign(slope) == -1.0f)
                                                || (mid_y < line_y && Mathf.Sign(slope) == 1.0f) 
                                                || (head_y == tail_y && mid_y < head_y);
                                    
                                    float flip_curve = flip ? -1.0f : 1.0f;
                                    
                                    float new_x = midpoint.x + flip_curve * b.locked_displacement * -Mathf.Sin(theta);
                                    float new_y = midpoint.y + flip_curve * b.locked_displacement * Mathf.Cos(theta);
                                    midpoint = new Vector3(new_x, new_y, 0);
                                }
                                
                                b.midpoint.UpdatePosition(midpoint);
                            } else {
                                // Send it to the move list
                                points_to_move.Add(b.midpoint);
                            }
                        }
                    }
                }
                
                foreach(Point p in points_to_move){
                    p.container.transform.position += displacement;
                }
            }
        }
    }

    void OnMouseDown(){
        // Don't do anything if the user is currently changing a value.
        if(arrow_grams_builder.accepting_input){
            return;
        }
        // Is this point an active point
        for(int i = 0 ; i < arrow_grams_builder.active_points.Count - 1 ; i++){
             if(this_point == arrow_grams_builder.active_points[i]){
                this_point_is_active = true;
                break;
            }
        }
        // Is this point a vertex
        foreach(Vertex v in arrow_grams_builder.vertices){
            if(this_point == v.vertex){
                arrow_grams_builder.hit_a_vertex = true;
                break;
            }
        }
    }

    void OnMouseUp(){
        // Don't do anything if the user is currently changing a value.
        if(arrow_grams_builder.accepting_input){
            return;
        }
        this_point_is_active = false;
        arrow_grams_builder.hit_a_vertex = false;
        drag_duration = 0;

        if(arrow_grams_builder.grid_toggle && !arrow_grams_builder.PointIsMidpoint(this_point)){
            // Snap to grid
            Vector3 rounded_mouse_position = new Vector3(Mathf.Round(20*Input.mousePosition.x/Screen.width)/20, Mathf.Round(20*Input.mousePosition.y/Screen.height)/20, 0);
            Vector3 displacement = rounded_mouse_position - this_point.container.gameObject.transform.position;
            this_point.UpdatePosition(rounded_mouse_position);

            foreach(Arrow a in arrow_grams_builder.arrows){
                if(this_point == a.head.vertex || this_point == a.tail.vertex){

                    // TODO This block is a duplicate of code above.. this is bad practice but works for now.
                    if(!(a.head == a.tail)){
                        // If it not a loop arrow
                        float head_x = a.head.vertex.container.transform.position.x;
                        float head_y = a.head.vertex.container.transform.position.y;
                        float tail_x = a.tail.vertex.container.transform.position.x;
                        float tail_y = a.tail.vertex.container.transform.position.y;
                        float mid_x = a.midpoint.container.transform.position.x;
                        float mid_y = a.midpoint.container.transform.position.y;
                        float slope = (head_y - tail_y)/(head_x - tail_x);
                        
                        Vector3 midpoint;
                        Vector3 true_midpoint = new Vector3((head_x + tail_x)/2, (head_y + tail_y)/2, 0);
                        
                        if(a.is_curved){
                            Vector3 head_pos = a.head.vertex.container.transform.position;
                            Vector3 tail_pos = a.tail.vertex.container.transform.position;
                            float theta = Mathf.Atan((head_pos - tail_pos).y / (head_pos - tail_pos).x);
                            
                            float line_y = slope * (mid_x - head_x) + head_y;
                            bool flip = (mid_y < line_y && Mathf.Sign(slope) == -1.0f)
                                || (mid_y < line_y && Mathf.Sign(slope) == 1.0f) 
                                    || (head_y == tail_y && mid_y < head_y);
                            
                            float flip_curve = flip ? -1.0f : 1.0f;
                            
                            float new_x = true_midpoint.x + flip_curve * a.locked_displacement * -Mathf.Sin(theta);
                            float new_y = true_midpoint.y + flip_curve * a.locked_displacement * Mathf.Cos(theta);
                            midpoint = new Vector3(new_x, new_y, 0);
                        } else {
                            midpoint = true_midpoint;
                        }
                        
                        a.midpoint.UpdatePosition(midpoint);
                    } else {
                        // Move the loop midpoint along with the dragged point.
                        a.midpoint.container.transform.position += displacement;
                    }


                    if(arrow_grams_builder.active_points.Count > 2){
                        for(int i = 0 ; i < arrow_grams_builder.active_points.Count; i++){
                             if(this_point != arrow_grams_builder.active_points[i]){
                                float x_value = arrow_grams_builder.active_points[i].container.gameObject.transform.position.x;
                                float y_value = arrow_grams_builder.active_points[i].container.gameObject.transform.position.y;
    
                                arrow_grams_builder.active_points[i].container.gameObject.transform.position = new Vector3(Mathf.Round(20*x_value)/20, Mathf.Round(20*y_value)/20, 0f);
                            }
                        }
                    }
                }
            }
        }
    }

    float FindSlope(float s1, float s2, float e1, float e2) {
        return (s2 - e2)/(s1 - e1);
    }
}
