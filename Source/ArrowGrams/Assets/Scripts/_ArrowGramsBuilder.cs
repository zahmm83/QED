


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

public class _ArrowGramsBuilder : MonoBehaviour
{
    public static _ArrowGramsBuilder Instance;

    // ==================================================================================
    // =============================  History Variables  ================================
    public class point_history_item{
        public Vector3  old_pos      { get; set; }
        public Vector3  old_offest   { get; set; }
        public string   old_text     { get; set; }

        public point_history_item (Vector3 position, string text, Vector3 offset = new Vector3()){
            old_pos = position;
            old_text = text;
            old_offest = offset;
        }

        public point_history_item (){
            old_pos = new Vector3();
            old_text = "0";
            old_offest = new Vector3();
        }
    }

    public class history_item{
        public Arrow                arrow               { get; set; }
        public Point                lone_point          { get; set; } 
        public point_history_item   head                { get; set; }
        public point_history_item   midpoint            { get; set; }
        public point_history_item   tail                { get; set; }
        public point_history_item   lone_point_details  { get; set; } 
        public Action               action              { get; set; }
        public List<history_item>   related_histories   { get; set; }

        // Constructor for Arrow Actions
        public history_item(Action action, Arrow arrow){
            this.action = action;
            this.arrow = arrow;
            head = new point_history_item(arrow.head.vertex.container.transform.position, arrow.head.vertex.container.GetComponent<GUIText>().text);
            midpoint = new point_history_item(arrow.midpoint.container.transform.position, arrow.midpoint.label.GetComponent<GUIText>().text, arrow.midpoint.label_offsets);
            tail = new point_history_item(arrow.tail.vertex.container.transform.position, arrow.tail.vertex.container.GetComponent<GUIText>().text);
        }
        
        // Constructor for Point Actions
        public history_item(Action action, Point point){
            this.action = action;
            this.arrow = null;
            lone_point = point;
            if(point.container != null){
                lone_point_details = new point_history_item(point.container.transform.position, point.container.GetComponent<GUIText>().text);
            } else {
                lone_point_details = new point_history_item();
            }
        }
    }
    
    public enum Action {Create_Arrow, Delete_Arrow, Create_Point, Delete_Point};
    public List<history_item> history = new List< history_item >();
    public int history_index = -1;
    public history_item current_history_item { 
        get{
            return history[history_index];
        }
    }

    public List<history_item> undo_history = new List<history_item>();
    public int undo_history_index = -1;
    public history_item current_undo_history_item { 
        get{
            return undo_history[undo_history_index];
        }
    }
    
    // ==================================================================================
    // ===========================  Arrow and Point Lists  ==============================
    public List<Arrow> arrows = new List<Arrow>();
    public Arrow last_arrow {
        get{ return arrows[arrows.Count - 1]; }
    }
    public List<Arrow> destroyed_arrows = new List<Arrow>();


    public List<Point> points = new List<Point>();
    public Point first_point{
        get{ return points[0]; }
    }
    public Point last_point{
        get{ return points[points.Count-1]; }
    }
    public List<Point> mid_points = new List<Point>();
    public Point last_midpoint{
        get{ return mid_points[mid_points.Count-1]; }
    }
    public List<Point> active_points = new List<Point>();
    public List<Point> destroyed_points = new List<Point>();


    public List<Vertex> vertices = new List<Vertex>();
    public Vertex first_vertex{
        get{ return vertices[0]; }
    }
    public Vertex last_vertex {
        get{ return vertices[vertices.Count-1]; }
    }
    public Vertex second_last_vertex {
        get{ return vertices[vertices.Count-2]; }
    }


    public List<VertexPair> vertex_pair_list;
    public List<int> vertex_pair_values{
        get{
            return vertex_pair_list.Select(u => u.value).ToList();
        }
    }

    // ==================================================================================
    // ============================  Selection Variables  ===============================
    public Point active_point { get; set; }
    public Point sub_active_point { get; set; }
    public Point active_midpoint { get; set; }

    public bool hit_a_vertex = false;

    public Vector3 multi_select_start;
    public Vector3 mouse_position;
    public Vector3 multi_select_end;
    public bool multi_select_drag = false;


    // ==================================================================================
    // ========================  Point Interaction Variables  ===========================
    public bool create_new_arrow = false;
    public int drag_duration = 0;
    public Point closest_point = null;

    public int input_timer = 0;
    public bool accepting_input = false;

    public int position_timer = 0;
    public bool accepting_position = false;
    public VertexPair edited_vertexPair = null;
    public int locked_old_position;

    
    // ==================================================================================
    // ================================  UI Variables  ==================================
    public bool grid_toggle = false;
    public bool ui_toggle = true;
    public bool ui_load = false;
    public bool ui_save = false;
    public bool ui_confirm_load = false;
    public bool ui_confirm_save = false;
    public bool ui_confirm_new = false;
    public bool ui_show_options = false;
    public bool ui_show_mode_options = false;
    public bool show_solution = false;
    public bool show_solution_pg2 = false;

    public bool show_mutate_ui = false;
    public string mutate_degree;
    public string mutate_factor;

    public bool show_generate_ui = false;

    public bool hide_drag_points = false;
    public bool show_blank_points = false;
    public bool is_directed = true;


    // ==================================================================================
    // ===============================  Mode Variables  =================================
    public bool show_mode_ui = false;
    public enum mode {
        QED
    };
    public mode active_mode;


    // ==================================================================================
    // =============================  Save/Load Variables  ==============================
    public string current_folder;
    public string[] file_paths;
    public string[] directory_paths;
    string save_name = "";
    string folder_name = "";
    string load_path = "";
    string save_path = "";
    string quicksave_path = "";

    
    // ********************************************************************************************************************
    // -------------------------------------------- Setup and Frame by Frame ----------------------------------------------
    // ********************************************************************************************************************

    void Awake () {
        active_mode = mode.QED;
        
        mutate_degree = "0.00";
        mutate_factor = "1.00";

        vertex_pair_list = new List<VertexPair>();

        // Load/Save defaults.
        current_folder = Directory.GetCurrentDirectory() + "\\SavedDiagrams";
        file_paths = Directory.GetFiles(current_folder, "*.arwg");
        directory_paths = Directory.GetDirectories(current_folder);
    }

    void Update() {
        ColorPoints();
        
        if(input_timer > 75){
            ResetUserInput();
        }
        
        if(UserIsRenamingPoint()){
            RenamePoint();
        }
        
        if(ArrowNeedsToBeCreated()){
            // Note that this is a special case, other arrow creation is done directly on mouse click.
            CreateArrowBetweenExistingPoints();
        }

        if(accepting_input){
            input_timer++;
        } else if (ControlIsHeld()) {

            if(Input.GetKeyDown(KeyCode.S)){
                QuickSave();
            } 
            else if(Input.GetKeyDown(KeyCode.Z)){
                UndoLastAction();
            } 
            else if(Input.GetKeyDown(KeyCode.V)){
                RedoLastAction();
            } 
            else if(Input.GetKeyDown(KeyCode.R) && active_midpoint != null){
                FlipActiveArrow();
            }
            else if(Input.GetKeyDown(KeyCode.L)){
                ToggleShowingEmptyLabels();
            }
            else if(Input.GetKeyDown(KeyCode.D)){
                ToggleDragPoints();
            }
            else if (Input.GetKeyDown(KeyCode.H)){
                ui_toggle = !ui_toggle;
            }
            else if(Input.GetKeyDown(KeyCode.G)){
                grid_toggle = !grid_toggle;
            }
            else if(Input.GetKeyDown(KeyCode.UpArrow)){
                IncreaseLineWidth();
            }
            else if(Input.GetKeyDown(KeyCode.DownArrow)){
                DecreaseLineWidth();
            }
        }
        else if(DeleteIsHeld()){
            MarkActivePointsForDeletion();
        }

        DeleteMarkedPoints();
    }

    void LateUpdate(){
        if(ControlIsHeld() && Input.GetKeyDown(KeyCode.P)){
            PrintToFile();
        }

//        foreach(history_item h in history){
//        Debug.Log(current_history_item.lone_point_details.old_text);
//        Debug.Log(current_history_item.lone_point.container);
//        }

    }


    // ********************************************************************************************************************
    // ----------------------------------------------- Mouse Functionality ------------------------------------------------
    // ********************************************************************************************************************

    void OnMouseDrag(){
        if(accepting_input){
            return;
        }
        drag_duration++;
        if(ControlIsHeld()){
            multi_select_drag = true;
            mouse_position = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
        }
    }

    void OnMouseDown(){
        if(accepting_input){
            return;
        }
        // Start the multi select box
        if(ControlIsHeld()){
            multi_select_start = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
        }
    }

    void OnMouseUp(){
        if(accepting_input){
            return;
        }
        
        closest_point = null;
        float closest_distance = 100;
        FindAndSetClosestPoint(ref closest_distance);

        if(closest_distance <= 0.02 && drag_duration < 10){
            if(ControlIsHeld()){
                AddClosestPointToMultiSelection();
            } else {
                SetClosestPointAsActive();
            }

            if(ShiftIsHeld()){
                create_new_arrow = true;
            }
        } else {
            closest_point = null;
        }

        if(ShiftIsHeld()){
            Vertex active_vertex = PointIsAVertex(active_point);
            string point_letter = GetAvailableLetter();

            if(active_vertex == null){
                CreatePointInSpace(point_letter);
                AddHistoryItem(new history_item(Action.Create_Point, last_point));

            } else if(ActivePointExists() && DidNotHitAnotherVertex()){
                Arrow generated_arrow = CreateArrowFromActiveVertex(point_letter, active_vertex);
                AddHistoryItem(new history_item(Action.Create_Arrow, generated_arrow));
            }
        }

        if(ControlIsHeld()){
            SelectAllPointsInSelectBox();
        }

        // Clicking in open space without shift or control held          
        if(drag_duration < 10 && DidNotHitAnotherVertex() && !ShiftIsHeld() && !ControlIsHeld()){
            ClearActivePoints();
            CloseAllOptionMenus();
        }

        drag_duration = 0;
    }


    // ********************************************************************************************************************
    // -------------------------------------------------- Create the GUI --------------------------------------------------
    // ********************************************************************************************************************
        
    void OnGUI() {
        GUI.skin.label.fontSize = 14;
        
        if(ui_toggle){
            ShowMainMenuItems();
        }
        
        if(ui_show_options){
            ShowNewButton();
            ShowSaveButton();
            ShowLoadButton();
            ShowGridToggleButton();
            ShowMutateButton();
            // Only one mode in this version, so we'll hide this but keep it around incase more modes come.
            //ShowModeButton();
        }

        if(ShowQEDOptions()){
            ShowDragPointsToggleButton();
            ShowEmptyLabelsToggleButton();
            ShowDirectedToggleButton();
        }

        if(ui_confirm_new){
            ShowConfirmNewDialog();
        }

        if(ui_save){
            ShowSavePuzzleDialog();
        }
        if(ui_confirm_save){
            ShowConfirmOverwriteDialog();
        }

        if(ui_load){
            ShowLoadPuzzleDialog();
        }
        if(ui_confirm_load){
            ShowConfirmLoadDialog();
        }

        if(show_mutate_ui){
            ShowMutateUI();
        }

//        if(show_mode_ui){
//            ShowModeChangeUI();
//        }
    }


    // ****************************************************************************************************************
    // ------------------------------------------------ Helper Methods ------------------------------------------------
    // ****************************************************************************************************************

    // ==================================================================================
    // ===============================  Update Methods  =================================

    private void ColorPoints(){
        foreach(Point p in points){
            if(PointIsMidpoint(p)){
                if(p == active_midpoint){
                    p.container.GetComponent<GUITexture>().texture = Resources.Load("GreyPoint", typeof(Texture2D)) as Texture2D;
                    p.label.GetComponent<GUIText>().material.color = Color.grey;
                } else {
                    p.container.GetComponent<GUITexture>().texture = Resources.Load("Point", typeof(Texture2D)) as Texture2D;
                    p.label.GetComponent<GUIText>().material.color = Color.black;
                }
                
                if(accepting_input){
                    p.label.GetComponent<GUIText>().material.color = Color.grey;
                }
            } else {
                p.container.GetComponent<GUIText>().material.color = Color.black;
                if(active_points.Contains(p)){
                    p.container.GetComponent<GUIText>().material.color = Color.blue;
                }
                
                if(accepting_input){
                    p.container.GetComponent<GUIText>().material.color = Color.grey;
                }
            }
        }
        
        if(!accepting_input && sub_active_point != null && active_points.Count <= 1){
            sub_active_point.container.GetComponent<GUIText>().material.color = Color.red;
        }
        if(!accepting_input && active_point != null){
            active_point.container.GetComponent<GUIText>().material.color = Color.blue;
        }
        
        // If we're hiding UI, color everthing black
        if(!ui_toggle){
            foreach(Point p in points){
                if(PointIsMidpoint(p)){
                    p.container.GetComponent<GUITexture>().texture = Resources.Load("Point", typeof(Texture2D)) as Texture2D;
                    p.label.GetComponent<GUIText>().material.color = Color.black;
                } else {
                    p.container.GetComponent<GUIText>().material.color = Color.black;
                }
            }
        }
    }
    
    private void ToggleShowingEmptyLabels(){
        ui_show_mode_options = false;
        foreach(Point p in mid_points){
            if(p.label.GetComponent<GUIText>().text.Trim() == "" && show_blank_points){
                p.label.GetComponent<GUIText>().text = "__";
            } else if (p.label.GetComponent<GUIText>().text.Trim() == "__" && !show_blank_points) {
                p.label.GetComponent<GUIText>().text = "";
            }
        }
        show_blank_points = !show_blank_points;
    }
    
    private void ToggleDragPoints(){
        hide_drag_points = !hide_drag_points;
        ui_show_mode_options = false;
        if(hide_drag_points){
            foreach(Point p in mid_points){
                p.container.GetComponent<GUITexture>().enabled = false;
            }
        } else {
            foreach(Point p in mid_points){
                p.container.GetComponent<GUITexture>().enabled = true;
            }
        }
    }

    private void IncreaseLineWidth(){
        DrawFromCamera.line_width = Mathf.Min(DrawFromCamera.line_width + 0.001f, 0.008f);
        
        DrawFromCamera.basis_triangle = new Vector3[]{
            new Vector3(-0.025f * (DrawFromCamera.line_width*100 + 0.4f), -0.009f * (DrawFromCamera.line_width*100 + 0.4f), 0.0f), 
            new Vector3(-0.025f * (DrawFromCamera.line_width*100 + 0.4f), 0.009f * (DrawFromCamera.line_width*100 + 0.4f), 0.0f), 
            new Vector3(0.0f, 0.0f, 0.0f)
        };
    }

    private void DecreaseLineWidth(){
        DrawFromCamera.line_width = Mathf.Max(DrawFromCamera.line_width - 0.001f, 0.004f);
        
        DrawFromCamera.basis_triangle = new Vector3[]{
            new Vector3(-0.025f * (DrawFromCamera.line_width*100 + 0.4f), -0.009f * (DrawFromCamera.line_width*100 + 0.4f), 0.0f), 
            new Vector3(-0.025f * (DrawFromCamera.line_width*100 + 0.4f), 0.009f * (DrawFromCamera.line_width*100 + 0.4f), 0.0f), 
            new Vector3(0.0f, 0.0f, 0.0f)
        };
    }

    private void PrintToFile(){
        System.DateTime now = System.DateTime.Now;
        string date = now.Month.ToString() + "-" + now.Day.ToString() + "-" + now.Year.ToString() + "_" + now.Hour.ToString() + now.Minute.ToString() + now.Second.ToString();
        Application.CaptureScreenshot("Image_" + date + ".png");
    }

    private void ResetUserInput(){
        accepting_input = false;
        input_timer = 0;
        
        if(active_point != null && active_point.container.GetComponent<GUIText>().text.Trim() == ""){
            if(active_point.use_old_value){
                active_point.container.GetComponent<GUIText>().text = active_point.old_value;
                active_point.use_old_value = false;
            } else {
                active_point.container.GetComponent<GUIText>().text = show_blank_points ? "" : "__";
            }
        }
        
        if(active_midpoint != null && active_midpoint.label.GetComponent<GUIText>().text.Trim() == ""){
            if(active_midpoint.use_old_value){
                active_midpoint.label.GetComponent<GUIText>().text = active_midpoint.old_value;
                active_midpoint.use_old_value = false;
            } else {
                active_midpoint.label.GetComponent<GUIText>().text = show_blank_points ? "" : "__";
            }
        }
    }
    
    private bool UserIsRenamingPoint(){
        return Input.anyKey && Input.inputString != "" && ! ControlIsHeld() && ! DeleteIsHeld();
    }
    
    private void RenamePoint(){
        GUIText text_to_change = null;
        Point point_to_change = null;
        
        if(active_midpoint != null){
            text_to_change = active_midpoint.label.GetComponent<GUIText>();
            point_to_change = active_midpoint;
        } else {
            text_to_change = active_point.container.GetComponent<GUIText>();
            point_to_change = active_point;
        }
        
        if(!accepting_input){
            
            if (text_to_change.text == "__") {
                point_to_change.use_old_value = true;
            } else {
                point_to_change.old_value = text_to_change.text;
            }      
            
            int temp = 0;
            if(active_midpoint != null && int.TryParse(active_midpoint.label.GetComponent<GUIText>().text, out temp)){
                // If what was entered was a valid number then we need to save it to the proper vertex pair
                // We do this so that arrowgrams has a value regardless of what was entered.
                foreach(Arrow a in arrows){
                    if(active_midpoint == a.midpoint){
                        foreach(VertexPair vp in vertex_pair_list){
                            if(vp.parent_arrow == a){
                                // Save off the old value of the midpoint, 
                                // it will be saved in fallback_value in the vertex pair object.
                                vp.value = temp;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            
            text_to_change.text = "";
            
        }
        // Reset the timer every time an input is received
        input_timer = 0;
        accepting_input = true;
        text_to_change.text += Input.inputString;
    }
    
    private bool ArrowNeedsToBeCreated(){
        // True if the user tried to create a new arrow and we have correct info to create it.
        return create_new_arrow && active_point != null && sub_active_point != null;
    }
    
    private void CreateArrowBetweenExistingPoints(){
        // Snap an arrow between two existing vertices
        Vertex active_vertex = PointIsAVertex(active_point);
        Vertex sub_active_vertex = PointIsAVertex(sub_active_point);
        
        // Generate the new midpoint and create the arrow using active_vertex, sub_active_vertex, and the new midpoint.
        Point head = active_point;
        Point tail = sub_active_point;
        Vector3 midpoint = new Vector3((head.container.transform.position.x + tail.container.transform.position.x)/2
                                       ,(head.container.transform.position.y + tail.container.transform.position.y)/2, 0);
        float slope = FindSlope(head.container.transform.position.x, head.container.transform.position.y, tail.container.transform.position.x, tail.container.transform.position.y);
        
        GameObject generated_midpoint = new GameObject("generated_midpoint");
        AddGUITexture(generated_midpoint, Resources.Load("Point", typeof(Texture2D)) as Texture2D,
                      midpoint, new Vector3(0.02f, 0.02f, 1.0f), null, "PointBehaviour");
        GameObject midpoint_label = new GameObject("midpoint_label");
        AddGUIText(midpoint_label, "0", new Vector3(0.1f, 0.1f, 2.0f), null, "LabelBehaviour");
        last_point.label = midpoint_label;
        last_point.label_offsets = new Vector3(0.02f, Mathf.Sign(-(1/slope))*0.02f, 0.0f);
        last_point.label.transform.position = new Vector3(midpoint.x + 0.02f, midpoint.y + Mathf.Sign(-(1/slope))*0.02f, 0.0f);
        
        mid_points.Add(last_point);
        
        Arrow generated_arrow = new Arrow(active_vertex, sub_active_vertex, points[points.Count-1]);
        
        if(head == tail){
            // This should be a loop
            last_point.container.transform.position = new Vector3(last_point.container.transform.position.x + 0.03f, last_point.container.transform.position.y + 0.03f, last_point.container.transform.position.z);
            generated_arrow.locked_displacement = 0.0424f;
        }
        
        arrows.Add(generated_arrow);
        
        // Update history
        AddHistoryItem(new history_item(Action.Create_Arrow, generated_arrow));
        
        create_new_arrow = false;
    }
    
    private bool ControlIsHeld(){
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }
    private bool DeleteIsHeld(){
        return Input.GetKey(KeyCode.Delete) || Input.GetKey(KeyCode.Backspace);
    }
    
    private void QuickSave(){
        if(quicksave_path == ""){
            show_mutate_ui = false;
            show_solution_pg2 = false;
            show_solution = false;
            Destroy(GameObject.Find("popup_background"));
            
            current_folder = Directory.GetCurrentDirectory() + "\\SavedDiagrams";
            file_paths = Directory.GetFiles(current_folder, "*.arwg");
            directory_paths = Directory.GetDirectories(current_folder);
            ui_load = false;
            Destroy(GameObject.Find("popup_background"));
            ui_save = true;
            GameObject popup_background = new GameObject("popup_background");
            AddGUITexture(popup_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
                          new Vector3(0.5f, 0.475f, 2.0f), new Vector3(0.7f, 0.75f, 1.0f), null);
        } else {
            SavePuzzle(quicksave_path);
        }
    }
    
    private void FlipActiveArrow(){
        foreach(Arrow a in arrows){
            if(active_midpoint == a.midpoint){
                Vertex temp = a.head;
                a.head = a.tail;
                a.tail = temp;
            }
        }
    }
    
    private void UndoLastAction(){
        if(history_index != -1){
            if(current_history_item.action == Action.Delete_Arrow){

                // Rebuild the deleted arrow.
                bool head_exists = false;
                bool tail_exists = false;
                Vertex head = current_history_item.arrow.head;
                Vertex tail = current_history_item.arrow.tail;
                
                foreach(Vertex v in vertices){
                    // Check if the head still exists
                    if(v.vertex.container.transform.position == current_history_item.head.old_pos){
                        head = v;
                        head_exists = true;
                    }
                    // Check if the tail still exists
                    if(v.vertex.container.transform.position == current_history_item.tail.old_pos){
                        tail = v;
                        tail_exists = true;
                    }
                }
                
                if(!head_exists){
                    // Recreate the head if necessary
                    GameObject remade_point = new GameObject("remade_point");
                    AddGUIText(remade_point, current_history_item.head.old_text, current_history_item.head.old_pos, null, "PointBehaviour");
                    vertices.Add(new Vertex(last_point));
                    head = last_vertex;
                }
                
                if(!tail_exists){
                    // Recreate the tail if necessary
                    GameObject remade_point = new GameObject("remade_point");
                    AddGUIText(remade_point, current_history_item.tail.old_text, current_history_item.tail.old_pos, null, "PointBehaviour");
                    vertices.Add(new Vertex(last_point));
                    tail = last_vertex;
                }
                
                GameObject remade_midpoint = new GameObject("remade_midpoint");
                AddGUITexture(remade_midpoint, Resources.Load("Point", typeof(Texture2D)) as Texture2D,
                              current_history_item.midpoint.old_pos, new Vector3(0.02f, 0.02f, 1.0f), null, "PointBehaviour");
                GameObject remade_label = new GameObject("remade_label");
                AddGUIText(remade_label, current_history_item.midpoint.old_text, new Vector3(0.1f, 0.1f, 2.0f), null, "LabelBehaviour");
                
                last_point.label = remade_label;
                last_point.label_offsets = current_history_item.midpoint.old_offest;
                last_point.UpdateLabelPosition();
                
                mid_points.Add(last_point);
                
                // Recreate the arrow
                Arrow generated_arrow = new Arrow(head, tail, last_midpoint);
                generated_arrow.locked_displacement = current_history_item.arrow.locked_displacement;
                
                arrows.Add(generated_arrow);

                foreach(history_item h in current_history_item.related_histories){
                    h.arrow = generated_arrow;
                }
                AddUndoHistoryItem(new history_item(Action.Create_Arrow, generated_arrow));

                history_index--;
                
            } else if (current_history_item.action == Action.Create_Arrow){
                AddUndoHistoryItem(new history_item(Action.Delete_Arrow, current_history_item.arrow));
                // Delete the created arrow (so much easier than undoing a delete :)
                destroyed_arrows.Add(current_history_item.arrow);

                history_index--;
                
            } else if (current_history_item.action == Action.Delete_Point){
                GameObject generated_point = new GameObject("generated_point");
                AddGUIText(generated_point, current_history_item.lone_point_details.old_text, current_history_item.lone_point_details.old_pos, null, "PointBehaviour");
                vertices.Add(new Vertex(last_point));

                foreach(history_item h in current_history_item.related_histories){
                    h.lone_point = last_point;
                }
                AddUndoHistoryItem(new history_item(Action.Create_Point, last_point));

                history_index--;
                
            } else if (current_history_item.action == Action.Create_Point){
                AddUndoHistoryItem(new history_item(Action.Delete_Point, current_history_item.lone_point));
                destroyed_points.Add(current_history_item.lone_point);

                history_index--;
            }
        }
    }

    private void RedoLastAction(){
        if(undo_history_index != -1){
            if(current_undo_history_item.action == Action.Delete_Arrow){
                // Rebuild the deleted arrow.
                bool head_exists = false;
                bool tail_exists = false;
                Vertex head = current_undo_history_item.arrow.head;
                Vertex tail = current_undo_history_item.arrow.tail;
                
                foreach(Vertex v in vertices){
                    // Check if the head still exists
                    if(v.vertex.container.transform.position == current_undo_history_item.head.old_pos){
                        head = v;
                        head_exists = true;
                    }
                    // Check if the tail still exists
                    if(v.vertex.container.transform.position == current_undo_history_item.tail.old_pos){
                        tail = v;
                        tail_exists = true;
                    }
                }
                
                if(!head_exists){
                    // Recreate the head if necessary
                    GameObject remade_point = new GameObject("remade_point");
                    AddGUIText(remade_point, current_undo_history_item.head.old_text, current_undo_history_item.head.old_pos, null, "PointBehaviour");
                    vertices.Add(new Vertex(last_point));
                    head = last_vertex;
                }
                
                if(!tail_exists){
                    // Recreate the tail if necessary
                    GameObject remade_point = new GameObject("remade_point");
                    AddGUIText(remade_point, current_undo_history_item.tail.old_text, current_undo_history_item.tail.old_pos, null, "PointBehaviour");
                    vertices.Add(new Vertex(last_point));
                    tail = last_vertex;
                }
                
                GameObject remade_midpoint = new GameObject("remade_midpoint");
                AddGUITexture(remade_midpoint, Resources.Load("Point", typeof(Texture2D)) as Texture2D,
                              current_undo_history_item.midpoint.old_pos, new Vector3(0.02f, 0.02f, 1.0f), null, "PointBehaviour");
                GameObject remade_label = new GameObject("remade_label");
                AddGUIText(remade_label, current_undo_history_item.midpoint.old_text, new Vector3(0.1f, 0.1f, 2.0f), null, "LabelBehaviour");
                
                last_point.label = remade_label;
                last_point.label_offsets = current_undo_history_item.midpoint.old_offest;
                last_point.UpdateLabelPosition();
                
                mid_points.Add(last_point);
                
                // Recreate the arrow
                Arrow generated_arrow = new Arrow(head, tail, last_midpoint);
                generated_arrow.locked_displacement = current_undo_history_item.arrow.locked_displacement;
                
                arrows.Add(generated_arrow);
                
                foreach(history_item h in current_undo_history_item.related_histories){
                    h.arrow = generated_arrow;
                }
                
                AddHistoryItem(new history_item(Action.Create_Arrow, generated_arrow), false);
                undo_history_index--;
                
            } else if (current_undo_history_item.action == Action.Create_Arrow){
                // Delete the created arrow (so much easier than undoing a delete :)
                destroyed_arrows.Add(current_undo_history_item.arrow);
                
                AddHistoryItem(new history_item(Action.Delete_Arrow, current_undo_history_item.arrow), false);
                undo_history_index--;
                
            } else if (current_undo_history_item.action == Action.Delete_Point){
                GameObject generated_point = new GameObject("generated_point");
                AddGUIText(generated_point, current_undo_history_item.lone_point_details.old_text, current_undo_history_item.lone_point_details.old_pos, null, "PointBehaviour");
                vertices.Add(new Vertex(last_point));

                foreach(history_item h in current_undo_history_item.related_histories){
                    h.lone_point = last_point;
                }
                
                AddHistoryItem(new history_item(Action.Create_Point, last_point), false);
                undo_history_index--;
                
            } else if (current_undo_history_item.action == Action.Create_Point){
                destroyed_points.Add(current_undo_history_item.lone_point);
                
                AddHistoryItem(new history_item(Action.Delete_Point, current_undo_history_item.lone_point), false);
                undo_history_index--;
            }
        }
    }
    
    private void MarkActivePointsForDeletion(){
        // Mark selected items for deletion
        if(active_midpoint != null){
            foreach(Arrow a in arrows){
                if(a.midpoint == active_midpoint){
                    destroyed_arrows.Add(a);
                    
                    // Update history
                    AddHistoryItem(new history_item(Action.Delete_Arrow, a));
                    break;
                }
            }
        } else {
            for(int i = 0 ; i < active_points.Count ; i++){
                Point p = active_points[i];
                bool p_is_lone_point = true;
                foreach(Arrow a in arrows){
                    if(( p == a.head.vertex || p == a.tail.vertex || p == a.midpoint ) && !destroyed_arrows.Contains(a)){
                        p_is_lone_point = false;
                        destroyed_arrows.Add(a);
                        
                        // Update history
                        AddHistoryItem(new history_item(Action.Delete_Arrow, a));
                    }
                }
                destroyed_points.Add(p);
                
                if(p_is_lone_point){
                    // Add Delete_Point history item.
                    AddHistoryItem(new history_item(Action.Delete_Point, p));
                }
            }
        }
    }
    
    private void DeleteMarkedPoints(){
        // Actual deletion of marked arrows and points occurs down here
        foreach(Arrow a in destroyed_arrows){
            
            vertex_pair_list.Remove(a.vertex_pair);
            arrows.Remove(a);
            mid_points.Remove(a.midpoint);
            points.Remove(a.midpoint);
            active_points.Remove(a.midpoint);
            Destroy(a.midpoint.container.gameObject);
            Destroy(a.midpoint.label.gameObject);
            
            if(VertexIsIsolated(a.head)){
                vertices.Remove(a.head);
                points.Remove(a.head.vertex);
                active_points.Remove(a.head.vertex);
                Destroy(a.head.vertex.container.gameObject);
            }
            if(VertexIsIsolated(a.tail)){
                vertices.Remove(a.tail);
                points.Remove(a.tail.vertex);
                active_points.Remove(a.tail.vertex);
                Destroy(a.tail.vertex.container.gameObject);
            }
            
            ClearActivePoints();
        }
        
        if(destroyed_points.Count > 0){
            foreach(Point p in destroyed_points){
                Vertex deleted_vertex = PointIsAVertex(p);
                if(deleted_vertex != null){
                    vertices.Remove(deleted_vertex);
                }
                if(PointIsMidpoint(p)){
                    mid_points.Remove(p);
                }
                points.Remove(p);
                active_points.Remove(p);
                
                Destroy(p.container.gameObject);
            }
            
            ClearActivePoints();
        }
        
        // Reset the destroyed points and arrows lists.
        destroyed_points.Clear();
        destroyed_arrows.Clear();
    }

    
    // =======================================================================================
    // ===============================  On Mouse Up Methods  =================================

    private void FindAndSetClosestPoint(ref float closest_distance){
        // Find the closest point
        mouse_position = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
        foreach(Point p in points){
            float x_value = p.container.gameObject.transform.position.x;
            float y_value = p.container.gameObject.transform.position.y;
            float p_distance = Mathf.Sqrt(Mathf.Pow((mouse_position.x - x_value),2) + Mathf.Pow((mouse_position.y - y_value),2));

            float label_distance = 100;
            if(PointIsMidpoint(p)){
                float label_x_value = p.label.gameObject.transform.position.x;
                float label_y_value = p.label.gameObject.transform.position.y;
                label_distance = Mathf.Sqrt(Mathf.Pow((mouse_position.x - label_x_value),2) + Mathf.Pow((mouse_position.y - label_y_value),2));
            }

            float distance = Mathf.Min(p_distance, label_distance);
            if(distance < closest_distance){
                closest_distance = distance;
                closest_point = p;
            }
        }
    }
    
    private void AddClosestPointToMultiSelection(){
        if(PointIsMidpoint(closest_point)){
            active_midpoint = closest_point;
        } else {
            active_midpoint = null;
            active_point = closest_point;
            active_points.Add(active_point);
        }
    }
    
    private void SetClosestPointAsActive(){
        if(PointIsMidpoint(closest_point)){
            active_midpoint = closest_point;
        } else {
            active_midpoint = null;
            Point holder_point = active_point;
            active_point = closest_point;
            sub_active_point = holder_point;
            
            active_points.Clear();
            active_points.Add(active_point);
        }
    }
    
    private bool ShiftIsHeld(){
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }
    
    private string GetAvailableLetter(){
        string available_letter = "G";
        foreach(string letter in new List<string>{"A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"}){
            bool is_free = true;
            foreach(Vertex v in vertices){
                if(v.vertex.container.GetComponent<GUIText>().text == letter){
                    is_free = false;
                    break;
                }
            }
            if(is_free){
                available_letter = letter;
                break;
            }
        }
        return available_letter;
    }
    
    private void CreatePointInSpace(string point_letter){
        GameObject generated_point = new GameObject("generated_point");
        Vector3 position = new Vector3();
        if(grid_toggle){
            position = new Vector3(Mathf.Round(20*Input.mousePosition.x/Screen.width)/20, Mathf.Round(20*Input.mousePosition.y/Screen.height)/20, 0);
        } else {
            position = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
        }
        AddGUIText(generated_point, point_letter, position, null, "PointBehaviour");
        vertices.Add(new Vertex(last_point));
        active_point = last_point;
    }
    
    private bool DidNotHitAnotherVertex(){
        return !hit_a_vertex && closest_point == null;
    }
    
    private bool ActivePointExists(){
        return active_point.container.gameObject != null;
    }
    
    private Arrow CreateArrowFromActiveVertex(string point_letter, Vertex active_vertex){
        GameObject generated_point = new GameObject("generated_point");

        Vector3 position = new Vector3();
        if(grid_toggle){
            position = new Vector3(Mathf.Round(20*Input.mousePosition.x/Screen.width)/20, Mathf.Round(20*Input.mousePosition.y/Screen.height)/20, 0);
        } else {
            position = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
        }

        AddGUIText(generated_point, point_letter, position, null, "PointBehaviour");

        vertices.Add(new Vertex(last_point));
        
        Point head = last_vertex.vertex;
        Point tail = active_vertex.vertex;
        Vector3 midpoint = new Vector3((head.container.transform.position.x + tail.container.transform.position.x)/2
                                       ,(head.container.transform.position.y + tail.container.transform.position.y)/2, 0);
        float slope = FindSlope(head.container.transform.position.x, head.container.transform.position.y, tail.container.transform.position.x, tail.container.transform.position.y);
        
        GameObject generated_midpoint = new GameObject("generated_midpoint");
        AddGUITexture(generated_midpoint, Resources.Load("Point", typeof(Texture2D)) as Texture2D,
                      midpoint, new Vector3(0.02f, 0.02f, 1.0f), null, "PointBehaviour");
        GameObject midpoint_label = new GameObject("midpoint_label");
        AddGUIText(midpoint_label, "0", new Vector3(0.1f, 0.1f, 2.0f), null, "LabelBehaviour");
        last_point.label = midpoint_label;
        last_point.label_offsets = new Vector3(0.02f, Mathf.Sign(-(1/slope))*0.02f, 0.0f);
        last_point.label.transform.position = new Vector3(midpoint.x + 0.02f, midpoint.y + Mathf.Sign(-(1/slope))*0.02f, 0.0f);
        
        mid_points.Add(last_point);
        
        Arrow generated_arrow = new Arrow(last_vertex, active_vertex, last_midpoint);
        arrows.Add(generated_arrow);
        
        active_midpoint = null;
        active_points.Remove(active_point);
        active_point = head;
        sub_active_point = tail;
        active_points.Add(active_point);
        
        return generated_arrow;
    }
    
    private void SelectAllPointsInSelectBox(){
        multi_select_end = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
        multi_select_drag = false;
        
        float x_dist = Mathf.Abs (multi_select_end.x - multi_select_start.x);
        float y_dist = Mathf.Abs (multi_select_end.y - multi_select_start.y);
        
        if(x_dist >= 0.025 && y_dist >= 0.025){
            List<Point> selected_points = new List<Point>();
            // Find all the points that were surrounded by the selection box.
            foreach(Vertex v in vertices){
                if( (x_dist >= (Mathf.Abs(multi_select_end.x - v.vertex.container.gameObject.transform.position.x)) && x_dist >= (Mathf.Abs(multi_select_start.x - v.vertex.container.gameObject.transform.position.x)))
                   && ((y_dist >= (Mathf.Abs(multi_select_end.y - v.vertex.container.gameObject.transform.position.y))) && y_dist >= (Mathf.Abs(multi_select_start.y - v.vertex.container.gameObject.transform.position.y)))){
                    
                    selected_points.Add(v.vertex);
                }
            }
            // If any points were selected, move the new selected points to the active_points list.
            if(selected_points.Count > 0){
                active_points.Clear();
                active_points.AddRange(selected_points);
                active_point = active_points[0];
            }
        }
    }
    
    private void AddHistoryItem(history_item new_history_item, bool clear_undo_history = true){
        new_history_item.related_histories = new List<history_item>();
        foreach(history_item h in history){
            // If the arrow or point is in another history item, we need to save that relation to update out of date histories properly.
            if((h.arrow != null && h.arrow == new_history_item.arrow) || (h.lone_point != null && h.lone_point == new_history_item.lone_point)){
                new_history_item.related_histories.Add(h);
            }
        }

        history_index ++;
        if(history_index > history.Count-1){
            history.Add(new_history_item);
        } else {
            history[history_index] = new_history_item;
        }

        // The user did something new, so they should no longer be able to redo untill something else is undone.
        if(clear_undo_history){
            undo_history.Clear();
            undo_history_index = -1;
        }
    }
    
    private void AddUndoHistoryItem(history_item new_undo_history_item){
        new_undo_history_item.related_histories = new List<history_item>();
        foreach(history_item h in undo_history){
            // If the arrow or point is in another history item, we need to save that relation to update out of date histories properly.
            if((h.arrow != null && h.arrow == new_undo_history_item.arrow) || (h.lone_point != null && h.lone_point == new_undo_history_item.lone_point)){
                new_undo_history_item.related_histories.Add(h);
            }
        }
        undo_history_index ++;
        if(undo_history_index > undo_history.Count-1){
            undo_history.Add(new_undo_history_item);
        } else {
            undo_history[undo_history_index] = new_undo_history_item;
        }
    }
    
    private void ClearActivePoints(){
        active_points.Clear();
        active_midpoint = null;
        active_point = null;
        sub_active_point = null;
    }
    
    private void CloseAllOptionMenus(){
        Destroy(GameObject.Find("popup_background"));
        ui_show_options = false;
        ui_show_mode_options = false;
        show_mode_ui = false;
        show_mutate_ui = false;
    }

    
    // ==================================================================================
    // ===============================  On GUI Methods  =================================
        
    private void ShowMainMenuItems(){
        // =================================  Main Options  =================================
        if(GUI.Button ( new Rect(0, 0, 100, 25), "Options" ) ){
            GameObject popup_background = GameObject.Find("popup_background");
            // Don't let the options button be used when another popup is active.
            // exceptions are popups that are sub-menus (mutate and mode bothe have sub-menus)
            if(popup_background == null || show_mutate_ui || show_mode_ui || show_generate_ui){
                ui_show_options = !ui_show_options;
                ui_show_mode_options = false;
                show_generate_ui = false;
                show_mutate_ui = false;
                show_mode_ui = false;
                if(!ui_show_options){
                    Destroy(GameObject.Find("popup_background"));
                }
            }
        }
        
        // =============================  Mode Specific Options  ============================
        if(GUI.Button ( new Rect(100, 0, 130, 25), active_mode.ToString() ) ){
            GameObject popup_background = GameObject.Find("popup_background");
            if(popup_background == null || show_mutate_ui || show_mode_ui || show_generate_ui){
                ui_show_mode_options = !ui_show_mode_options;
                show_generate_ui = false;
                ui_show_options = false;
                show_mutate_ui = false;
                show_mode_ui = false;
                Destroy(GameObject.Find("popup_background"));
            }
        }
    }
    
    private bool LoadOrSaveIsOpen(){
        return ui_load || ui_save;
    }

    private void ShowNewButton(){
        if(GUI.Button ( new Rect(0, 25, 100, 25), "New" ) ){
            GameObject confirm_background = new GameObject("confirm_background");
            AddGUITexture(confirm_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
                          new Vector3(0.5f, 0.505f, 2.0f), new Vector3(0.33f, 0.21f, 1.0f), null);
            ui_confirm_new = true;
            CloseAllOptionMenus();
        }
    }
    
    private void ShowSaveButton(){
        if(GUI.Button ( new Rect(0, 50.085f, 100, 25), "Save" ) ){
            // Make sure the solution window gets closed.
            ui_show_options = false;
            show_mutate_ui = false;
            show_solution_pg2 = false;
            show_solution = false;
            Destroy(GameObject.Find("popup_background"));
            
            current_folder = Directory.GetCurrentDirectory() + "\\SavedDiagrams";
            file_paths = Directory.GetFiles(current_folder, "*.arwg");
            directory_paths = Directory.GetDirectories(current_folder);
            ui_load = false;
            Destroy(GameObject.Find("popup_background"));
            ui_save = true;
            GameObject popup_background = new GameObject("popup_background");
            AddGUITexture(popup_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
                          new Vector3(0.5f, 0.475f, 2.0f), new Vector3(0.7f, 0.75f, 1.0f), null);
        }
    }
    
    private void ShowLoadButton(){
        if(GUI.Button ( new Rect(0, 75.125f, 100, 25), "Load" ) ){
            // Make sure the solution window gets closed.
            ui_show_options = false;
            show_solution_pg2 = false;
            show_solution = false;
            show_mutate_ui = false;
            Destroy(GameObject.Find("popup_background"));
            
            current_folder = Directory.GetCurrentDirectory() + "\\SavedDiagrams";
            file_paths = Directory.GetFiles(current_folder, "*.arwg");
            directory_paths = Directory.GetDirectories(current_folder);
            ui_save = false;
            Destroy(GameObject.Find("popup_background"));
            ui_load = true;
            GameObject popup_background = new GameObject("popup_background");
            AddGUITexture(popup_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
                          new Vector3(0.5f, 0.5f, 2.0f), new Vector3(0.7f, 0.7f, 1.0f), null);
        }
    }
    
    private void ShowGridToggleButton(){
        if(GUI.Button ( new Rect(0, 100, 100, 25), "Toggle Grid" ) ){
            ui_show_options = false;
            show_mutate_ui = false;
            show_mode_ui = false;
            Destroy(GameObject.Find("popup_background"));
            grid_toggle = !grid_toggle;
        }
    }
    
    private void ShowMutateButton(){
        if(!LoadOrSaveIsOpen() && GUI.Button( new Rect(0, 125, 100, 25), "Mutate" )){
            mutate_degree = "0.00";
            mutate_factor = "1.00";
            Destroy(GameObject.Find("popup_background"));
            show_mode_ui = false;
            show_generate_ui = false;
            show_mutate_ui = !show_mutate_ui;
//            if(show_mutate_ui){
//                GameObject popup_background = new GameObject("popup_background");
//                AddGUITexture(popup_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
//                              new Vector3(0.1625f, 0.735f, 2.0f), new Vector3(0.125f, 0.2f, 1.0f), null);
//            }
        }
    }
    
    private void ShowModeButton(){
        if(GUI.Button ( new Rect(0, 150, 100, 25), "Mode" ) ){
            Destroy(GameObject.Find("popup_background"));
            show_mutate_ui = false;
            show_generate_ui = false;
            show_mode_ui = !show_mode_ui;
//            if(show_mode_ui){
//                GameObject popup_background = new GameObject("popup_background");
//                AddGUITexture(popup_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
//                              new Vector3(0.165f, 0.695f, 2.0f), new Vector3(0.125f, 0.2f, 1.0f), null);
//            }
        }
    }
    
    private bool ShowQEDOptions(){
        return ui_show_mode_options && active_mode == mode.QED;
    }
    
    private void ShowDragPointsToggleButton(){
        if(GUI.Button ( new Rect(100, 25, 130, 25), "Drag Points: " + (hide_drag_points ? "Off" : "On") ) ){
            ToggleDragPoints();
        }
    }
    
    private void ShowEmptyLabelsToggleButton(){
        if(GUI.Button ( new Rect(100, 50, 130, 25), "Empty Labels: " + (show_blank_points ? "Off" : "On") ) ){
            ToggleShowingEmptyLabels();
        }
    }
    
    private void ShowDirectedToggleButton(){
        if(GUI.Button ( new Rect(100, 75, 130, 25), "Directed: " + (is_directed ? "True" : "False") ) ){
            ui_show_mode_options = false;
            is_directed = ! is_directed;
        }
    }
    
    private void ShowMutateUI(){
        GUI.color = Color.black;
        
        GUI.Label(new Rect(110, 130, 100, 25), "Rotate: ");
        GUI.Label(new Rect(110, 155, 100, 25), "Scale: ");

        GUI.Box(new Rect(100, 125, 120, 100), "");
        
        GUI.color = Color.white;
        
        mutate_degree = GUI.TextField(new Rect(160, 130, 50, 25), mutate_degree );
        mutate_factor = GUI.TextField(new Rect(160, 155, 50, 25), mutate_factor );
        
        if(GUI.Button ( new Rect(110, 190, 100, 25), "Apply" ) ){
            // Remove the UI
            Destroy(GameObject.Find("popup_background"));
            ui_show_options = false;
            show_mutate_ui = false;
            
            // Don't do anything if there is no active point (mutate relies on having one) to base mutations around.
            if(active_point == null){
                return;
            }
            
            float x_offset = 0;
            float y_offset = 0;
            
            float validated_degree = 0.0f;
            float validated_factor = 1.0f;
            
            Matrix rotation_matrix = new Matrix(new List<List<float>>(){
                new List<float>(){
                    1.0f,
                    0.0f
                },
                new List<float>(){
                    0.0f,
                    1.0f
                }
            });
            
            if(float.TryParse(mutate_degree, out validated_degree)){
                rotation_matrix = new Matrix(new List<List<float>>(){
                    new List<float>(){
                        Mathf.Cos (validated_degree*(Mathf.PI/180)),
                        (-1)*Mathf.Sin (validated_degree*(Mathf.PI/180))
                    },
                    new List<float>(){
                        Mathf.Sin (validated_degree*(Mathf.PI/180)),
                        Mathf.Cos (validated_degree*(Mathf.PI/180))
                    }
                });
            }
            
            if(float.TryParse(mutate_factor, out validated_factor)){
                x_offset = active_point.container.transform.position.x - active_point.container.transform.position.x*validated_factor;
                y_offset = active_point.container.transform.position.y - active_point.container.transform.position.y*validated_factor;
            }
            
            foreach(Point p in points){
                Matrix position_matrix = new Matrix(new List<List<float>>(){
                    new List<float>(){
                        p.container.transform.position.x-0.5f
                    },
                    new List<float>(){
                        p.container.transform.position.y-0.5f
                    }
                });
                
                Matrix new_position = rotation_matrix.mMultiply(position_matrix);
                
                p.container.transform.position = new Vector3(
                    (new_position.rows[0][0] + 0.5f)*validated_factor + x_offset
                    , (new_position.rows[1][0] + 0.5f)*validated_factor + y_offset
                    , 0);
                if(PointIsMidpoint(p)){
                    p.UpdateLabelPosition();
                }
            }
        }
    }
    
    private void ShowLoadPuzzleDialog(){
        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width*0.4f,Screen.height*0.15f,Screen.width*0.3f,Screen.height*0.05f), current_folder);
        GUI.color = Color.white;
        
        if(GUI.Button(new Rect(Screen.width*0.825f, Screen.height*0.15f, Screen.width*0.025f, Screen.height*0.03f), "X" )){
            ui_load = false;
            Destroy(GameObject.Find("popup_background"));
        }
        
        GUI.Box(new Rect(Screen.width*0.3f,Screen.height*0.19f,Screen.width*0.54f,Screen.height*0.65f),"");
        
        if(GUI.Button ( new Rect(Screen.width*0.16f, Screen.height*0.2f, Screen.width*0.13f, Screen.height*0.05f), "<< Back" )){
            string[] split_directory = current_folder.Split('\\');
            current_folder = "";
            
            if(split_directory.Length > 2){
                // Rebuild the directory path, excluding the last piece
                for(int i = 0 ; i < split_directory.Length - 1 ; i++){
                    current_folder += split_directory[i];
                    if(i != split_directory.Length-2){
                        current_folder += "\\";
                    }
                }
            } else if(split_directory.Length == 2){
                // This should go to your Drive folder.
                current_folder += split_directory[0] + "\\";
            }
            
            file_paths = Directory.GetFiles(current_folder, "*.arwg");
            directory_paths = Directory.GetDirectories(current_folder);
        }
        
        for(int i = 0 ; i < directory_paths.Length ; i++){
            string[] split_directory = directory_paths[i].Split('\\');
            
            if(GUI.Button ( new Rect(Screen.width*0.16f, Screen.height*0.2f + Screen.height*0.06f*(i+1), Screen.width*0.13f, Screen.height*0.05f), split_directory[split_directory.Length - 1] ) ){
                current_folder = directory_paths[i];
                file_paths = Directory.GetFiles(current_folder, "*.arwg");
                directory_paths = Directory.GetDirectories(current_folder);
            }
        }
        for(int i = 0 ; i < file_paths.Length ; i++){
            string[] split_file = file_paths[i].Split('\\');
            
            if(GUI.Button ( new Rect(Screen.width*0.31f + Screen.width*0.14f*(i/10), Screen.height*0.22f + Screen.height*0.06f*(i%10), Screen.width*0.13f, Screen.height*0.05f), split_file[split_file.Length - 1] ) ){
                
                ui_confirm_load = true;
                GameObject confirm_background = new GameObject("confirm_background");
                AddGUITexture(confirm_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
                              new Vector3(0.5f, 0.505f, 2.0f), new Vector3(0.33f, 0.21f, 1.0f), null);
                ui_load = false;
                Destroy(GameObject.Find("popup_background"));
                load_path = file_paths[i];
            }
        }
    }
    
    private void ShowConfirmLoadDialog(){
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 15;
        GUI.skin.box.fontSize = 15;
        GUI.Box(new Rect(Screen.width*0.34f,Screen.height*0.4f,Screen.width*0.32f,Screen.height*0.19f), "Confirm Load");
        GUI.Label(new Rect(Screen.width*0.35f,Screen.height*0.45f,Screen.width*0.36f,Screen.height*0.24f), "Loading will cause you to lose any unsaved data, continue?");
        if(GUI.Button(new Rect(Screen.width*0.5f,Screen.height*0.52f,Screen.width*0.07f,Screen.height*0.05f) ,"Yes")){
            LoadPuzzle(load_path);
            load_path = "";
            ui_confirm_load = false;
            Destroy(GameObject.Find("confirm_background"));
        }
        if(GUI.Button(new Rect(Screen.width*0.58f,Screen.height*0.52f,Screen.width*0.07f,Screen.height*0.05f) ,"No")){
            ui_confirm_load = false;
            Destroy(GameObject.Find("confirm_background"));
            ui_load = true;
            GameObject popup_background = new GameObject("popup_background");
            AddGUITexture(popup_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
                          new Vector3(0.5f, 0.5f, 2.0f), new Vector3(0.7f, 0.7f, 1.0f), null);
        }
    }
    
    private void ShowSavePuzzleDialog(){
        GUI.color = Color.black;
        GUI.skin.textField.alignment = TextAnchor.MiddleLeft;
        GUI.Label(new Rect(Screen.width*0.4f,Screen.height*0.15f,Screen.width*0.3f,Screen.height*0.05f), current_folder);
        GUI.Label(new Rect(Screen.width*0.3f,Screen.height*0.85f,Screen.width*0.05f,Screen.height*0.05f), "Save As:");
        save_name = GUI.TextField(new Rect(Screen.width*0.35f,Screen.height*0.845f,Screen.width*0.4f,Screen.height*0.05f), save_name);
        GUI.color = Color.white;
        
        if(GUI.Button(new Rect(Screen.width*0.825f, Screen.height*0.15f, Screen.width*0.025f, Screen.height*0.03f), "X" )){
            ui_save = false;
            Destroy(GameObject.Find("popup_background"));
        }
        
        if(GUI.Button (new Rect(Screen.width*0.755f,Screen.height*0.845f,Screen.width*0.085f,Screen.height*0.05f), "Save")){
            SavePuzzle(current_folder+"\\"+save_name+".arwg");
        }
        
        GUI.Box(new Rect(Screen.width*0.3f,Screen.height*0.19f,Screen.width*0.54f,Screen.height*0.65f),"");
        
        if(GUI.Button ( new Rect(Screen.width*0.16f, Screen.height*0.2f, Screen.width*0.13f, Screen.height*0.05f), "<< Back" )){
            string[] split_directory = current_folder.Split('\\');
            current_folder = "";
            
            if(split_directory.Length > 2){
                // Rebuild the directory path, excluding the last piece
                for(int i = 0 ; i < split_directory.Length - 1 ; i++){
                    current_folder += split_directory[i];
                    if(i != split_directory.Length-2){
                        current_folder += "\\";
                    }
                }
            } else if(split_directory.Length == 2){
                // This should go to your Drive folder, but you're not allowed to go any farther back.
                current_folder += split_directory[0] + "\\";
            }
            
            file_paths = Directory.GetFiles(current_folder, "*.arwg");
            directory_paths = Directory.GetDirectories(current_folder);
        }
        
        for(int i = 0 ; i < directory_paths.Length ; i++){
            string[] split_directory = directory_paths[i].Split('\\');
            
            if(GUI.Button ( new Rect(Screen.width*0.16f, Screen.height*0.2f + Screen.height*0.06f*(i+1), Screen.width*0.13f, Screen.height*0.05f), split_directory[split_directory.Length - 1] ) ){
                current_folder = directory_paths[i];
                file_paths = Directory.GetFiles(current_folder, "*.arwg");
                directory_paths = Directory.GetDirectories(current_folder);
            }
        }
        
        folder_name = GUI.TextField(new Rect(Screen.width*0.16f,Screen.height*0.2f + Screen.height*0.06f*(directory_paths.Length+1),Screen.width*0.13f,Screen.height*0.05f), folder_name);
        if(GUI.Button (new Rect(Screen.width*0.175f,Screen.height*0.2f + Screen.height*0.06f*(directory_paths.Length+2),100,Screen.height*0.05f), "Create Folder")){
            Directory.CreateDirectory(current_folder+"\\"+folder_name);
            directory_paths = Directory.GetDirectories(current_folder);
            folder_name = "";
        }
        
        for(int i = 0 ; i < file_paths.Length ; i++){
            string[] split_file = file_paths[i].Split('\\');
            
            if(GUI.Button ( new Rect(Screen.width*0.31f + Screen.width*0.14f*(i/10), Screen.height*0.22f + Screen.height*0.06f*(i%10), Screen.width*0.13f, Screen.height*0.05f), split_file[split_file.Length - 1] ) ){
                ui_confirm_save = true;
                GameObject confirm_background = new GameObject("confirm_background");
                AddGUITexture(confirm_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
                              new Vector3(0.5f, 0.505f, 2.0f), new Vector3(0.33f, 0.21f, 1.0f), null);
                ui_save = false;
                Destroy(GameObject.Find("popup_background"));
                save_path = file_paths[i];
                //SavePuzzle(file_paths[i]);
            }
        }
    }

    private void ShowConfirmNewDialog(){
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 15;
        GUI.skin.box.fontSize = 15;
        GUI.Box(new Rect(Screen.width*0.34f,Screen.height*0.4f,Screen.width*0.32f,Screen.height*0.19f), "Confirm New");
        GUI.Label(new Rect(Screen.width*0.35f,Screen.height*0.45f,Screen.width*0.32f,Screen.height*0.24f), "Creating a new puzzle will cause you to lose any unsaved data, continue?");

        if(GUI.Button(new Rect(Screen.width*0.5f,Screen.height*0.52f,Screen.width*0.07f,Screen.height*0.05f) ,"Yes")){
            active_points.AddRange(points.Where(x => !PointIsMidpoint(x)));
            MarkActivePointsForDeletion();
            quicksave_path = "";
            save_name = "";
            save_path = "";
            Destroy(GameObject.Find("confirm_background"));
            ui_confirm_new = false;
        }
        if(GUI.Button(new Rect(Screen.width*0.58f,Screen.height*0.52f,Screen.width*0.07f,Screen.height*0.05f) ,"No")){
            Destroy(GameObject.Find("confirm_background"));
            ui_confirm_new = false;
        }
    }

    private void ShowConfirmOverwriteDialog(){
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 15;
        GUI.skin.box.fontSize = 15;
        GUI.Box(new Rect(Screen.width*0.34f,Screen.height*0.4f,Screen.width*0.32f,Screen.height*0.19f), "Confirm Save");
        GUI.Label(new Rect(Screen.width*0.35f,Screen.height*0.45f,Screen.width*0.36f,Screen.height*0.24f), "Saving will over-write this file, continue?");
        if(GUI.Button(new Rect(Screen.width*0.5f,Screen.height*0.52f,Screen.width*0.07f,Screen.height*0.05f) ,"Yes")){
            SavePuzzle(save_path);
            save_name = "";
            save_path = "";
            ui_confirm_save = false;
            Destroy(GameObject.Find("confirm_background"));
        }
        if(GUI.Button(new Rect(Screen.width*0.58f,Screen.height*0.52f,Screen.width*0.07f,Screen.height*0.05f) ,"No")){
            ui_confirm_save = false;
            Destroy(GameObject.Find("confirm_background"));
            ui_save = true;
            GameObject popup_background = new GameObject("popup_background");
            AddGUITexture(popup_background, Resources.Load("PopupBackground", typeof(Texture2D)) as Texture2D,
                          new Vector3(0.5f, 0.475f, 2.0f), new Vector3(0.7f, 0.75f, 1.0f), null);
        }
    }
    
    private void ShowModeChangeUI(){
        var centeredStyle = GUI.skin.GetStyle("Label");
        centeredStyle.alignment = TextAnchor.UpperCenter;
        GUI.color = Color.black;

        GUI.Box(new Rect(100, 150, 120, 100), "");

        GUI.Label(new Rect(110, 160, 100, 25), "Current Mode", centeredStyle);
        GUI.Label(new Rect(110, 185, 100, 25), active_mode.ToString(), centeredStyle);
        GUI.Label(new Rect(110, 210, 100, 25), "Change Mode", centeredStyle);

        centeredStyle.alignment = TextAnchor.UpperLeft;
        GUI.color = Color.white;
        
        int num_modes = 0;
        foreach (mode m in mode.GetValues(typeof(mode)))
        {
            if(active_mode != m){
                if(GUI.Button ( new Rect(Screen.width*0.111f, Screen.height*(0.32f + num_modes * 0.04f), 100, 25), m.ToString() ) ){
                    active_mode = m;
                    Destroy(GameObject.Find("popup_background"));
                    show_mode_ui = false;
                    ui_show_options = false;
                    
                    if(active_mode == mode.QED){
                        hide_drag_points = false;
                    } 
                }
                num_modes++;
            }
        }
    }

    private void LoadPuzzle(string path){
        // Save off the last used path, for auto-save feature.
        quicksave_path = path;

        // Clear the screen.
        foreach(Arrow a in arrows){
            destroyed_arrows.Add(a);
        }
        points.Clear();
        arrows.Clear();
        mid_points.Clear();
        vertices.Clear();
    
        string[] readText = File.ReadAllLines(path);
        foreach (string s in readText)
        {
            string[] split_string = s.Split(' ');
            // PV - point and vertex
            if(split_string[0] == "PV"){
                GameObject loaded_point = new GameObject("Point 1");
                AddGUIText(loaded_point, split_string[1], new Vector3(float.Parse(split_string[2]), float.Parse(split_string[3]), float.Parse(split_string[4])), null, "PointBehaviour");
                Vertex vertex = new Vertex(last_point);
                vertices.Add(vertex);
            }
    
            // PM - point and midpoint
            else if(split_string[0] == "PM"){
                Vector3 midpoint = new Vector3(float.Parse(split_string[2]), float.Parse(split_string[3]), float.Parse(split_string[4]));
                GameObject loaded_point = new GameObject("Point 1");
                AddGUITexture(loaded_point, Resources.Load("Point", typeof(Texture2D)) as Texture2D,
                              midpoint, new Vector3(0.02f, 0.02f, 1.0f), null, "PointBehaviour");
                //                loaded_point.guiText.pixelOffset = new Vector2(float.Parse(s.Split(' ')[5]), float.Parse(s.Split(' ')[6]));
                GameObject midpoint_label = new GameObject("midpoint_label");
                AddGUIText(midpoint_label, split_string[1], new Vector3(0.1f, 0.1f, 2.0f), null, "LabelBehaviour");
                last_point.label = midpoint_label;

                // Allow for old save files to work semi-properly.
                float width_to_use = Mathf.Abs(float.Parse(split_string[5])) > 1.0 ? float.Parse(split_string[5])/Screen.width : float.Parse(split_string[5]);
                float height_to_use = Mathf.Abs(float.Parse(split_string[6])) > 1.0 ? float.Parse(split_string[6])/Screen.width : float.Parse(split_string[6]);

                last_point.label_offsets = new Vector3(width_to_use, height_to_use, 0.0f);
                last_point.UpdateLabelPosition();

                mid_points.Add(last_point);
            }
    
            // A is for Arrow!
            else if(s.Split(' ')[0] == "A"){
                Arrow loaded_arrow = new Arrow(vertices[int.Parse(split_string[1])], vertices[int.Parse(split_string[2])], mid_points[int.Parse(split_string[3])]);

                // Allow for old save files to work semi-properly.
                if(split_string.Length > 4){
                    loaded_arrow.locked_displacement = float.Parse(split_string[4]);
                }
                arrows.Add(loaded_arrow);
            }
        }
    
        // Set the default active vertex
        active_midpoint = null;
        active_point = vertices[0].vertex;
        sub_active_point = vertices[1].vertex;
        active_points.Add(active_point);

        ui_load = false;
        Destroy(GameObject.Find("popup_background"));

    }

    private void SavePuzzle(string path){
        // Save off the last used path, for auto-save feature.
        quicksave_path = path;

        List<string> puzzle = new List<string>();
        foreach(Vertex v in vertices){
            string point_string = "PV " + v.vertex.container.GetComponent<GUIText>().text + " " + v.vertex.container.transform.position.x + " " + v.vertex.container.transform.position.y + " " + v.vertex.container.transform.position.z;
            puzzle.Add(point_string);
        }
        foreach(Point m in mid_points){
            string point_string = "PM " + m.label.GetComponent<GUIText>().text + " " + m.container.transform.position.x + " " + m.container.transform.position.y + " " + m.container.transform.position.z + " " + m.label_offsets.x + " " + m.label_offsets.y;
            puzzle.Add(point_string);
        }

        foreach(Arrow a in arrows){
            int head_index = -1;
            int tail_index = -1;
            int mid_index = -1;
            for(int i = 0 ; i < vertices.Count ; i++){
                if(a.head == vertices[i]){
                    head_index = i;
                }
                if(a.tail == vertices[i]){
                    tail_index = i;
                }
            }
            for(int i = 0 ; i < mid_points.Count ; i++){
                if(a.midpoint == mid_points[i]){
                    mid_index = i;
                }
            }
            if(head_index != -1 && tail_index != -1 && mid_index != -1){
                string arrow_string = "A " + head_index + " " + tail_index + " " + mid_index + " " + a.locked_displacement;
                puzzle.Add(arrow_string);
            } else {
                Debug.Log("Something went wrong");
                Debug.Log(head_index);
                Debug.Log(tail_index);
                Debug.Log(mid_index);
            }
        }

        File.WriteAllLines(path, puzzle.ToArray());
        ui_save = false;
        Destroy(GameObject.Find("popup_background"));
    }

    
    // =================================================================================
    // ===============================  Other Methods  =================================

    private float FindSlope(float s1, float s2, float e1, float e2) {
        return (s2 - e2)/(s1 - e1);
    }

    private bool VertexIsIsolated(Vertex v){
        bool is_isolated = true;
        foreach(Arrow a in arrows){
            if(a.head == v || a.tail == v){
                is_isolated = false;
            }
        }
        return is_isolated;
    }

    private Vertex PointIsAVertex(Point p){
        Vertex the_vertex = null;
        foreach(Vertex v in vertices){
            if(p == v.vertex){
                the_vertex = v;
            }
        }
        return the_vertex;
    }

    public bool PointIsMidpoint(Point p){
        bool is_midpoint = false;
        foreach(Point m in mid_points){
            if(p == m){
                is_midpoint = true;
            }
        }
        return is_midpoint;
    }

    private void AddGUIText(GameObject obj, string text, Vector3 loc, Transform parent){
        obj.AddComponent<GUIText>();
        obj.GetComponent<GUIText>().text = text;
        obj.GetComponent<GUIText>().material.color = Color.black;
        obj.GetComponent<GUIText>().anchor = TextAnchor.MiddleCenter;
        obj.GetComponent<GUIText>().fontSize = 16;
        obj.GetComponent<GUIText>().fontStyle = FontStyle.Bold;
        obj.transform.position = loc;
        obj.transform.parent = parent;
    }
    private void AddGUIText(GameObject obj, string text, Vector3 loc, Transform parent, string cOne){
        AddGUIText(obj, text, loc, parent);
        if(cOne == "PointBehaviour"){
            obj.AddComponent<PointBehaviour>();
        } else if (cOne == "LabelBehaviour") {
            obj.AddComponent<LabelBehaviour>();
        }
        //UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(obj, "Assets/Scripts/_ArrowGramsBuilder.cs (1568,9)", cOne);
    }
//    private void AddGUIText(GameObject obj, string text, Vector3 loc, Transform parent, string cOne, string cTwo){
//        AddGUIText(obj, text, loc, parent);
////        UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(obj, "Assets/Scripts/_ArrowGramsBuilder.cs (1572,9)", cOne);
////        UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(obj, "Assets/Scripts/_ArrowGramsBuilder.cs (1573,9)", cTwo);
//    }

    private void AddGUITexture(GameObject obj, Texture2D texture, Vector3 loc, Vector3 scale, Transform parent) {
        obj.AddComponent<GUITexture>();
        obj.GetComponent<GUITexture>().texture = texture;
        obj.transform.position = loc;
        obj.transform.localScale = scale;
        obj.transform.parent = parent;
    }
    private void AddGUITexture(GameObject obj, Texture2D texture, Vector3 loc, Vector3 scale, Transform parent, string cOne) {
        AddGUITexture(obj, texture, loc, scale, parent);
        if(cOne == "PointBehaviour"){
            obj.AddComponent<PointBehaviour>();
        } else if (cOne == "LabelBehaviour") {
            obj.AddComponent<LabelBehaviour>();
        }
    }
//    private void AddGUITexture(GameObject obj, Texture2D texture, Vector3 loc, Vector3 scale, Transform parent, string cOne, string cTwo) {
//        AddGUITexture(obj, texture, loc, scale, parent);
////        UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(obj, "Assets/Scripts/_ArrowGramsBuilder.cs (1589,9)", cOne);
////        UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(obj, "Assets/Scripts/_ArrowGramsBuilder.cs (1590,9)", cTwo);
//    }
}