using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Arrow  {

	public Vertex head {get; set;}
    public Vertex tail {get; set;}
    public Point midpoint {get; set;}
    public VertexPair vertex_pair { get; set; }
    public float locked_displacement { get; set; }

    private _ArrowGramsBuilder arrow_grams_builder;

    public Arrow(Vertex head, Vertex tail, Point midpoint){
        this.head = head;
        this.tail = tail;
        this.midpoint = midpoint;
        this.arrow_grams_builder = GameObject.Find("Background").GetComponent("_ArrowGramsBuilder") as _ArrowGramsBuilder;
        vertex_pair = new VertexPair(this);
        arrow_grams_builder.vertex_pair_list.Add(vertex_pair);
    }

    public bool is_curved { 
        get{ 
            float head_x = head.vertex.container.transform.position.x;
            float head_y = head.vertex.container.transform.position.y;
            float tail_x = tail.vertex.container.transform.position.x;
            float tail_y = tail.vertex.container.transform.position.y;
            float mid_x = midpoint.container.transform.position.x;
            float mid_y = midpoint.container.transform.position.y;
            float mid_displacement = Mathf.Sqrt( Mathf.Pow((head_x+tail_x)/2 - mid_x,2) + Mathf.Pow((head_y+tail_y)/2 - mid_y,2));
            return mid_displacement > 0.01f;
        } 
    }

    public float curve_displacement { 
        get{ 
            float head_x = head.vertex.container.transform.position.x;
            float head_y = head.vertex.container.transform.position.y;
            float tail_x = tail.vertex.container.transform.position.x;
            float tail_y = tail.vertex.container.transform.position.y;
            float mid_x = midpoint.container.transform.position.x;
            float mid_y = midpoint.container.transform.position.y;
            float mid_displacement = Mathf.Sqrt( Mathf.Pow((head_x+tail_x)/2 - mid_x,2) + Mathf.Pow((head_y+tail_y)/2 - mid_y,2));
            return mid_displacement;
        } 
    }
}
