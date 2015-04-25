using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DrawFromCamera : MonoBehaviour
{
    private _ArrowGramsBuilder arrow_grams_builder;
	public Material mat;
    public static float line_width;
    public static float stop_short_length;
    public Vector3[] basis_rectangle;
    public static Vector3[] basis_triangle;
	
	void Awake() {
        arrow_grams_builder = GameObject.Find("Background").GetComponent("_ArrowGramsBuilder") as _ArrowGramsBuilder;
        // Set the basis rectangle to be the square centered on the origin with side length 1
        basis_rectangle = new Vector3[]{new Vector3(-0.5f, 0.5f, 0.0f), new Vector3(-0.5f, -0.5f, 0.0f), new Vector3(0.5f, -0.5f, 0.0f), new Vector3(0.5f, 0.5f, 0.0f)};
        // Also set the basis triangle, used as the arrow head. The "point" is on the origin.
        basis_triangle = new Vector3[]{new Vector3(-0.025f, -0.009f, 0.0f), new Vector3(-0.025f, 0.009f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f)};
        stop_short_length = 0.027f;
        line_width = 0.0064f;
    }
	
	void OnPostRender() {
	    if (!mat) {
	        Debug.LogError("Please Assign a material in the inspector");
	        return;
	    }
	    DrawArrows();

        // Draw the selection box if left control is being held.
        if(arrow_grams_builder.multi_select_drag && Input.GetKey(KeyCode.LeftControl)){
            float x_dist = Mathf.Abs (arrow_grams_builder.mouse_position.x - arrow_grams_builder.multi_select_start.x);
            float y_dist = Mathf.Abs (arrow_grams_builder.mouse_position.y - arrow_grams_builder.multi_select_start.y);

            if(x_dist >= 0.025 && y_dist >= 0.025){
                DrawSelectionBox();
            }
        }

        if(arrow_grams_builder.grid_toggle){
            DrawGrid();
        }

//        if(arrow_grams_builder.active_mode == _ArrowGramsBuilder.mode.QED && !arrow_grams_builder.hide_drag_points){
//            foreach(Point p in arrow_grams_builder.mid_points){
//                float x = p.container.transform.position.x;
//                float y = p.container.transform.position.y;
//                DrawCircle(new Vector2(x, y), 0.00025f, null, new Color(1.0f,1.0f,1.0f,1.0f));
//            }
//        }
	}

    void DrawGrid(){
        float size = 0.001f;
        for(int i = 1 ; i < 20 ; i++){
            for(int j = 1 ; j < 20 ; j++){
                GL.PushMatrix();
                mat.SetPass(0);
                GL.LoadOrtho();
                GL.Begin( GL.QUADS );
                GL.Color( Color.grey );
                GL.Vertex3(0.05f*i-size, 0.05f*j+size, 0f);
                GL.Vertex3(0.05f*i+size, 0.05f*j+size, 0f);
                GL.Vertex3(0.05f*i+size, 0.05f*j-size, 0f);
                GL.Vertex3(0.05f*i-size, 0.05f*j-size, 0f);
                GL.End();
                GL.PopMatrix();
            }
        }
    }

    void DrawArrows() {
        foreach(Arrow a in arrow_grams_builder.arrows){
            if(a.tail.vertex.container.gameObject != null && a.head.vertex.container.gameObject != null){
                float head_x = a.head.vertex.container.transform.position.x;
                float head_y = a.head.vertex.container.transform.position.y;
                float tail_x = a.tail.vertex.container.transform.position.x;
                float tail_y = a.tail.vertex.container.transform.position.y;
                
                float mid_x = a.midpoint.container.transform.position.x;
                float mid_y = a.midpoint.container.transform.position.y;

                float mid_displacement = a.locked_displacement; // Mathf.Sqrt( Mathf.Pow((head_x+tail_x)/2 - mid_x,2) + Mathf.Pow((head_y+tail_y)/2 - mid_y,2));

                // Select the correct draw method.
                if(mid_displacement > 0.01f){
                    if(a.tail == a.head){
                        // The arrow/line is looping back to itself.
                        Vector2 center = new Vector2((a.tail.vertex.container.transform.position.x + a.midpoint.container.transform.position.x)/2, 
                                                     (a.tail.vertex.container.transform.position.y + a.midpoint.container.transform.position.y)/2);
                        DrawCircle(center, mid_displacement/2, a.head.vertex, new Color(1.0f,1.0f,1.0f,1.0f));
                    } else {
                        // Curved arrow/line
                        float slope = (head_y - tail_y)/(head_x - tail_x);
                        float line_y = slope * (mid_x - head_x) + head_y;
                        bool flip = (mid_y > line_y && Mathf.Sign(slope) == -1.0f) || 
                                    (mid_y < line_y && Mathf.Sign(slope) == 1.0f) || 
                                    (head_y == tail_y && mid_y < head_y);

                        DrawCurve(head_x, head_y, tail_x, tail_y, new Color(1.0f,1.0f,1.0f,1.0f), mid_displacement, flip);
                    }
                } else {
                    // Just a regular arrow
                    DrawArrow(new Vector2(tail_x, tail_y), new Vector2(head_x, head_y), new Color(1.0f,1.0f,1.0f,1.0f), false);
                }              
            }
        }
    }

    void DrawLine(Vector2 start, Vector2 end, Color color, bool stop_short){
        float slope = (start.y - end.y)/(start.x - end.x);
        float theta = Mathf.Atan(-1/slope);
        float length = FindDistance(start, end) - (stop_short ? 2 * stop_short_length : 0);

        List<Vector3> draw_points = new List<Vector3>();
        
        // Transform the basis rectangle to be the rectangle that we need
        foreach(Vector3 v in basis_rectangle){
            // Scale, Rotate, and Translate all rolled into one.
            float transformed_x = (start.x + end.x) / 2.0f + v.x * line_width * Mathf.Cos(theta) - v.y * length * Mathf.Sin(theta);
            float transformed_y = (start.y + end.y) / 2.0f + v.x * line_width * Mathf.Sin(theta) + v.y * length * Mathf.Cos(theta);
            
            draw_points.Add(new Vector3(transformed_x, transformed_y, 0.0f));
        }
        
        GL.PushMatrix();
        mat.SetPass(0);
        GL.LoadOrtho();
        GL.Begin( GL.QUADS );
        GL.Color( color );
        foreach(Vector3 v in draw_points){
            GL.Vertex3(v.x, v.y, 0.0f);
        }
        GL.End();
        GL.PopMatrix();
    }

    void DrawArrowhead(Vector2 tail, Vector2 head, Color color, bool flip){
        // The x and y offsets to push the arrow away from the point, but keep it in line.
        float slope = (tail.y - head.y)/(tail.x - head.x);
        float theta = Mathf.Atan((head - tail).y / (head - tail).x);
        List<Vector3> draw_points = new List<Vector3>();

        float mag = FindDistance(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, slope, 0.0f));
        float ssd_x = (1 / mag) * stop_short_length / 1.5f;
        float ssd_y = (slope / mag) * stop_short_length / 1.5f;

        int quadrant_offset = 1;
        float flip_rotation = 0;

        // Quadrant/Angle Adjustments
        // -- quadrant_offset: if -1 flips the arrowhead to the 'opposite' side of the point.
        // -- flip_rotation: adds extra 180 degree rotation/s if needed.
        if(tail.x < head.x && tail.y < head.y){
            // Quadrant 1
            quadrant_offset = -1;
        }
        else if(tail.x > head.x && tail.y < head.y){
            // Quadrant 2
            flip_rotation = Mathf.PI;
        }
        else if(tail.x > head.x && tail.y > head.y){
            // Quadrant 3
            flip_rotation = Mathf.PI;
        }
        else if(tail.x < head.x && tail.y > head.y){
            // Quadrant 4
            quadrant_offset = -1;
        } 
        else if(tail.y == head.y){
            // Horizontal
            if(tail.x > head.x){
                flip_rotation += Mathf.PI;
            } else {
                quadrant_offset *= -1;
            }
        } 
        else if(tail.x == head.x){
            // Vertical - compensates for infinite slopes
            if(tail.y > head.y){
                ssd_y = stop_short_length * 0.7f;
            } else {
                ssd_y = -stop_short_length * 0.7f;
            }
        }

        // Arrowheads on curved lines need to be flipped... not sure why.
        if(flip){
            flip_rotation += Mathf.PI;
        }

        foreach(Vector3 v in basis_triangle){
            // Rotate and Translate, no scaling since it's already the size we want
            float transformed_x = head.x + v.x * Mathf.Cos(theta + flip_rotation) - v.y * Mathf.Sin(theta + flip_rotation) + quadrant_offset * ssd_x;
            float transformed_y = head.y + v.x * Mathf.Sin(theta + flip_rotation) + v.y * Mathf.Cos(theta + flip_rotation) + quadrant_offset * ssd_y;

            draw_points.Add(new Vector3(transformed_x, transformed_y, 0.0f));
        }
        
        GL.PushMatrix();
        mat.SetPass(0);
        GL.LoadOrtho();
        GL.Begin( GL.TRIANGLES );
        GL.Color( color );
        foreach(Vector3 v in draw_points){
            GL.Vertex3(v.x, v.y, 0.0f);
        }
        GL.End();
        GL.PopMatrix();
    }

    void DrawCircle(Vector2 center, float radius, Point avoid_point, Color color){
        float x_prev, y_prev, x_next, y_next, dist_prev, dist_next;
        float x_avoid = avoid_point == null ? -1.0f : avoid_point.container.transform.position.x;
        float y_avoid = avoid_point == null ? -1.0f : avoid_point.container.transform.position.y;

        for(int i = 1; i < 101; i++){
            x_prev = center.x + radius * Mathf.Cos ((2*Mathf.PI/100)*(i-1));
            y_prev = center.y + radius * Mathf.Sin ((2*Mathf.PI/100)*(i-1));
            
            x_next = center.x + radius * Mathf.Cos ((2*Mathf.PI/100)*(i));
            y_next = center.y + radius * Mathf.Sin ((2*Mathf.PI/100)*(i));

            dist_prev = Mathf.Sqrt(Mathf.Pow(x_avoid - x_prev,2) + Mathf.Pow(y_avoid - y_prev,2));
            dist_next = Mathf.Sqrt(Mathf.Pow(x_avoid - x_next,2) + Mathf.Pow(y_avoid - y_next,2));

            if(dist_next > 0.02f && dist_prev > 0.02f){
                float slope = (y_prev - y_next)/(x_prev - x_next);
                float length = 0.01f;

                float theta = Mathf.Atan(-1/slope);

                // Take the line_width by length rectangle centered on the origin and transform it to the desired rotation and position.
                // ---------------------------------------------------------------------------------------------------------------------
                // Can probably be reduced like DrawLine to be drawn from the base rectangle. However, these lines are not
                // center based like DrawLine does it so it cannot be used directly without modification.
                // TODO Modify so it can use DrawLine directly
                float x1 = x_prev + -line_width * 0.5f * Mathf.Cos(theta) - length * Mathf.Sin(theta);
                float y1 = y_prev + -line_width * 0.5f * Mathf.Sin(theta) + length * Mathf.Cos(theta);
                
                float x2 = x_prev + -line_width * 0.5f * Mathf.Cos(theta) - -length * Mathf.Sin(theta);
                float y2 = y_prev + -line_width * 0.5f * Mathf.Sin(theta) + -length * Mathf.Cos(theta);
                
                float x3 = x_prev + line_width * 0.5f * Mathf.Cos(theta) - length * Mathf.Sin(theta);
                float y3 = y_prev + line_width * 0.5f * Mathf.Sin(theta) + length * Mathf.Cos(theta);
                
                float x4 = x_prev + line_width * 0.5f * Mathf.Cos(theta) - -length * Mathf.Sin(theta);
                float y4 = y_prev + line_width * 0.5f * Mathf.Sin(theta) + -length * Mathf.Cos(theta);

                GL.PushMatrix();
                mat.SetPass(0);
                GL.LoadOrtho();
                GL.Begin( GL.QUADS );
                GL.Color( color );
                GL.Vertex3(x1,y1, 0f);
                GL.Vertex3(x2,y2, 0f);
                GL.Vertex3(x4,y4, 0f);
                GL.Vertex3(x3,y3, 0f);
                GL.End();
                GL.PopMatrix();
            } 
        }
    }

    void DrawArrow(Vector2 start, Vector2 end, Color color, bool flip_arrowhead){
        DrawLine(start, end, color, true);
        if(arrow_grams_builder.is_directed){
            DrawArrowhead(start, end, color, flip_arrowhead);
        }
    }

    List<Vector2> GeneratePointList(Vector2 start, Vector2 end, float height, bool flip){
        List<Vector2> point_list = new List<Vector2>();
        float del_x = Mathf.Abs(start.x - end.x);
        float del_y = Mathf.Abs(start.y - end.y);
        float mid_x = (start.x + end.x)/2;
        float mid_y = (start.y + end.y)/2;
        int direction = flip ? 1 : -1;
        float d = Mathf.Sqrt(Mathf.Pow(del_x,2) + Mathf.Pow(del_y,2));
        float num_points = 200;
        float increment = d/num_points;

        // generate points to draw through.
        for(int i = 0 ; i <= num_points ; i++){
            float orig_x = (-d/2)+i*increment;
            float orig_y = direction*height/(Mathf.Pow(d,2)/4)*(orig_x-d/2)*(orig_x+d/2);

            Vector2 new_point = new Vector2();

            // The following equations use a closed form equation to calculate the transformation of moving the origin based point to the point we want it at.
            // It involves a rotational transform to orient the curve about the slope of the line and a translational transform to position it correctly in the screen space.

            // TODO The equation used depends on if the slope of the line is positive or negative, I just split it into quadrants to check slope, 
            // TODO this probably less effecient than getting the slope itself then checking positive or negative so change this later.

            // Quadrant 1 curve orientation.
            if(start.x >= end.x && start.y >= end.y){
                new_point = new Vector2((del_x/d)*orig_x - (del_y/d)*orig_y + mid_x, (del_y/d)*orig_x + (del_x/d)*orig_y + mid_y);
            }
            // Quadrant 2 curve orientation.
            else if(start.x < end.x && start.y > end.y){
                new_point = new Vector2(-1*(del_x/d)*orig_x - (del_y/d)*orig_y + mid_x, (del_y/d)*orig_x + -1*(del_x/d)*orig_y + mid_y);
            }
            // Quadrant 3 vector orientation.
            else if(start.x <= end.x && start.y <= end.y){
                new_point = new Vector2((del_x/d)*orig_x - (del_y/d)*orig_y + mid_x, (del_y/d)*orig_x + (del_x/d)*orig_y + mid_y);
            }
            // Quadrant 4 vector orientation.
            else if(start.x > end.x && start.y < end.y){
                new_point = new Vector2(-1*(del_x/d)*orig_x - (del_y/d)*orig_y + mid_x, (del_y/d)*orig_x + -1*(del_x/d)*orig_y + mid_y);
            }

            if(FindDistance(start, new_point) > stop_short_length && FindDistance(end, new_point) > stop_short_length){
                point_list.Add(new_point);
            }
        }

        return point_list;
    }


    void DrawCurve(float x1, float y1, float x2, float y2, Color color, float height, bool flip){
        List<Vector2> points = GeneratePointList(new Vector2(x1, y1), new Vector2(x2, y2), height, flip);
        if((x1 >= x2 && y1 >= y2) || (x1 < x2 && y1 > y2)){
            // Quadrant 3 or 4
            points.Reverse();
        }

        for(int i = points.Count - 1; i > 0; i--){
            if(i != 1){
                DrawLine(new Vector2(points[i-1].x,  points[i-1].y), new Vector2(points[i].x, points[i].y), color, false);
            } else if(arrow_grams_builder.is_directed) {
                DrawArrowhead(new Vector2(points[i-1].x,  points[i-1].y), new Vector2(points[i].x, points[i].y), color, true);
            }
        }
    }

    void DrawSelectionBox(){
        GL.PushMatrix();
        mat.SetPass(0);
        GL.LoadOrtho();
        GL.Begin(GL.LINES);
        GL.Color(Color.blue);
        GL.Vertex(arrow_grams_builder.multi_select_start);
        GL.Vertex(new Vector3(arrow_grams_builder.multi_select_start.x, arrow_grams_builder.mouse_position.y, 0) );

        GL.Vertex(arrow_grams_builder.multi_select_start);
        GL.Vertex(new Vector3(arrow_grams_builder.mouse_position.x, arrow_grams_builder.multi_select_start.y, 0) );

        GL.Vertex(new Vector3(arrow_grams_builder.mouse_position.x, arrow_grams_builder.multi_select_start.y, 0) );
        GL.Vertex(arrow_grams_builder.mouse_position);

        GL.Vertex(new Vector3(arrow_grams_builder.multi_select_start.x, arrow_grams_builder.mouse_position.y, 0) );
        GL.Vertex(arrow_grams_builder.mouse_position);
        GL.End();
        GL.PopMatrix();
    }

    float FindDistance(Vector2 p1, Vector2 p2){
        return Mathf.Sqrt(Mathf.Pow(p1.x-p2.x, 2) + Mathf.Pow(p1.y-p2.y, 2));
    }
    float FindDistance(Vector3 p1, Vector3 p2){
        return Mathf.Sqrt(Mathf.Pow(p1.x-p2.x, 2) + Mathf.Pow(p1.y-p2.y, 2));
    }
}

