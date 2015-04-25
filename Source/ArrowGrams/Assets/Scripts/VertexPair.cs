using UnityEngine;
using System.Collections;

public class VertexPair
{
    public Arrow parent_arrow { get; set; }
    public string letter_pair { 
        get{
            return parent_arrow.tail.vertex.container.GetComponent<GUIText>().text + parent_arrow.head.vertex.container.GetComponent<GUIText>().text;
        } 
    }

    public int value { 
        get{
            int actual_value = 0;
            if(!int.TryParse(parent_arrow.midpoint.label.GetComponent<GUIText>().text, out actual_value)){
                actual_value = fallback_value;
            }
            return actual_value;
        }
        set{
            // Set fallback to the previous value, then set the new value
            int value_if_int = 0;
            if(int.TryParse(parent_arrow.midpoint.label.GetComponent<GUIText>().text, out value_if_int)){
                fallback_value = value_if_int;
            }
            parent_arrow.midpoint.label.GetComponent<GUIText>().text = value.ToString();
        }
    }

    public string value_string { 
        get { return parent_arrow.midpoint.label.GetComponent<GUIText>().text; } 
        set { 
            parent_arrow.midpoint.label.GetComponent<GUIText>().text = value;
            int temp = 0;
            if(int.TryParse(value, out temp)){
                this.value = temp;
            }
        } 
    }


    public int fallback_value { get; set; }

    public bool is_conflicted { get; set; }
    public bool is_blanked { get; set; }
    public bool is_part_of_triple { get; set; }
    
    public VertexPair(Arrow arrow){
        this.parent_arrow = arrow;
        this.fallback_value = 0;
        this.is_conflicted = false;
        this.is_blanked = false;
        this.is_part_of_triple = false;
    }
}

