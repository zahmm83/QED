# QED
A software project in Unity3D for quickly and easily designing and drawing vertex-edge graphs.


Quick Reference Guide

Create a new vertex – Shift + click in space with no other vertex selected
Create a new edge – Shift + click in space or on another vertex with a vertex selected
Create a loop – Shift + click on the active vertex
Move a vertex – Drag the vertex to wherever you want it
Curve an edge – Drag the midpoint dot
Multi Select – Ctrl + click on multiple vertices or Ctrl + click and drag to box select
Delete – Select the vertex or edge you want deleted and press backspace or delete
Rename a Label – Select a label and type. All other actions are locked until color returns
Reverse an Edge – Select the edge and press Ctrl + R
Undoing – Press Ctrl + Z
Redoing – Press Ctrl + V
Quick Saving – Press Ctrl + S
Toggle Grid – Press Ctrl + G 
Toggle the Showing of Drag Points – Press Ctrl + D
Toggle the Showing of Empty Labels – Press Ctrl + L
Print to File – Press Ctrl + P - it will be saved to the Data file that comes with the .exe
Print to Clipboard – Press Alt + Print Screen (PrtScr) - the window must be selected/active
Change Line Widths – Ctrl + Up Arrow or Down Arrow


 
Starting the program
No install is necessary to run this program. All you have to do is:
1.)	Extract the zip where you’d like to keep it.
2.)	Run the QED.exe file.
Please note that the QED.exe file needs to be in the same directory as the “QED_Data” and “SavedDiagrams” folders. When you extract the zip it will do this but be aware that moving the QED.exe file away from these folders will break the program.

Using the program
When you start the program you will begin with a blank screen with two menu buttons in the top left.
 

There will be more about these menu buttons and what is within them later (on page 6 and farther). First we cover the basics of the program and how to create and manipulate diagrams.





Vertices and Edges
To start making your diagram hold the Shift key and click anywhere on the screen. This will create a vertex wherever you click. Notice that this vertex is colored blue, this means that it is the “active vertex.” 
 
When a vertex is blue then holding shift and clicking elsewhere on the screen creates an edge from the active vertex to the clicked position. If you click in space it will create a new vertex for the edge but if you click on another vertex it will create an edge between the two vertices.
       Vertex B was created for the edge.			               	The edge was snapped between C and A.
                                                    

Clicking a vertex sets it as the active vertex while clicking in space clears all active vertices and edges. You can move a vertex by clicking and dragging it around the screen.
The red vertex is the previously active vertex. It shows you the last created edge and its associated vertices. 
Midpoints
On every edge there is a midpoint which serves as the control point for created curved edges. Clicking and dragging the midpoint will curve the edge. The edge remains curved even when the vertices are moved. The label associated to the midpoint can also be dragged to be positioned. A selected/active midpoint is colored grey.
              Curving an edge.			      Moving a midpoint label.          	               An active midpoint.
                           	         
 
Stacked Edges
You may create two edges using the same two vertices. This looks a little funny at first since the two edges overlap but once you curve them they look fine. 
        Stacked edges both going the same and opposite directions.		        Stacked edges once they have been curved.
                           		 
Loops
You may also create an edge from a vertex to itself. which results in a loop. This works the same as snapping an edge between two vertices: just select the vertex and shift click on it again to create an edge back to itself. To resize the loop just drag the midpoint/edge around.
 
Multi Selection
You may group multiple vertices and when doing so moving one moves them all. To do multi selection you may either hold control and select additional vertices or, while holding control, drag a box around the vertices you want selected.
     Two selected vertices.			The two vertices have moved together when dragging.
  			          


Reversing Edges
When an edge is selected you can press Ctrl + R to reverse the direction of the edge.

Labels
Labels are the text associated to a vertex or midpoint. For a vertex this label is statically placed where the vertex is. For an edge/midpoint the label is detached and can be placed wherever you want as described above. 
Both types of labels are renamed in similar ways. Just select the vertex or midpoint you want to change the label for and start typing. If a midpoint and vertex are both selected then the midpoint will be altered.
Changing the label of a vertex.			      Changing the label of an edge/midpoint.
  			 
As soon as you start typing every label will turn grey. This is because you cannot alter anything about the diagram while it’s expecting input. Once coloring has returned you will be able to use the program as normal.
Entering just a space while changing a label will create an underscore area where others can fill in the blank. The value that was there previously is not lost. Editing this label and entering just a space again will recover the old value.
        	 

Known bug: Currently backspace does not function while you are editing a label. If you make a mistake you have to wait for the color to return and start over.

Deleting
Deleting is simple, just select the edge or vertex you want deleted and press the backspace or delete key. If an edge and vertex are both selected the edge gets deleted.
When deleting an edge the only thing that is removed is that edge. The one exception to this is if deleting an edge leaves a vertex isolated, then that isolated vertex is also deleted.
When deleting a vertex all edges attached to it are removed. Just as when deleting a single edge if this leaves any vertices isolated then they are also removed. 
Deleting with more than one vertex selected deletes the entire selection.
    The graph we’re deleting from.		       The result of deleting A.	                 The result of deleting the EF edge.
                     

Undoing and Redoing
By using the standard command, Ctrl + Z, you can undo your last action or actions.
By using the standard command, Ctrl + Y, you can redo your undone action or actions.
Known bug: There is an issue with it skipping an undo press when you place a point in space followed by connecting an arrow to that point.

Change line widths
By using Ctrl + UpArrow and Ctrl + DownArrow you can change the widths of the edge lines.

Print Screen
By using Ctrl + P the window will be printed to the Data file that comes with the .exe
By using the Windows command Alt + PrtScr (Print Screen) you can print the active window to the clipboard.
Toolbar Actions
Clicking on the Options button will reveal the following menu. We’ll cover what these do one at a time.
 
Save
This button brings up the save diagram dialog.
 
To the left is a folder navigation and creation menu. Click on folders or the back button to navigate. The darker area to the right shows all “.arwg” files in the current folder. To save you can either click on an already saved diagram (this will overwrite the existing one) or enter a name at the bottom and click save. 
Quicksave – You can use the command Ctrl + S to quick save a loaded diagram or one that has been saved at least once before. Otherwise this command brings up the save dialog.

Load
This button brings up the load diagram dialog.
 
This is simplified version of the save dialog, it has folder navigation to the left and will show any “.arwg” files in the current folder to the right. Just click on a “.arwg” file on the right to load it.

Toggle Grid
Shortcut – Press Ctrl + G
This button will display a grid of points on the screen. If the grid is toggled on and a vertex is moved, the vertex will snap to the grid point that is nearest it. This allows you to line up vertices and form right angles with edges without too much hassle To turn the grid off just return to the options menu and click “Toggle Grid” again.
 

Mutate
When you click this button it will display a mutate options menu
 

Entering values other than those shown and clicking Apply will rotate and scale every point by the specified ammounts. Rotations are centered on the center of the screen. Scaling is done about the selected point to give you a little more control. Rotations are performed first.
Known bug: A vertex must be selected for either of these transformations to occur. This makes sense for scaling since it needs to know what to scale about but rotations should be able to be made with no point selected.
Mode
When you click this button it will display a mode options menu
 

This will show you what mode you are currently in and let you switch modes by clicking its respective button. This isn’t used so much at the moment but is included with expansion of the program in mind.
Generate
This is totally broken and has been removed for now. It needs to be refactored to use the new midpoint/label system.

Mode Specific Options
To the right of the Options button is the mode specific options button. This menu will change depending on what mode the program is currently in.
 
QED Options
Drag Points: On/Off
Shortcut – Ctrl + D
On – The drag points for arrows will be shown.
Off – The drag points for arrows will be hidden.

Empty Labels: On/Off 
Shortcut – Ctrl + L
On – Midpoint labels with no text will be shown as “__”.
Off – Midpoint labels with no text will have no display

Directed: True/False 
True – The arrowheads will be shown.
False – The arrowheads will be hidden so just lines connect vertices.

