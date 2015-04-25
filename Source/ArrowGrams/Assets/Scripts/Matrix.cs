using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Matrix {

    // Variables
    public List<List<float>> rows { get; set; }
    public List<List<float>> columns { get; set; }

    // These are populated only for matricies that are created by rref of another matrix.
    public List<int> basic_variables { get; set; }
    public List<int> free_variables { get; set; }

    // Properties
    public int m {
        get { return rows.Count; }
    }
    public int n {
        get { return columns.Count; }
    }

    // Constructor
    public Matrix(List<List<float>> rows){
        this.rows = rows;
        UpdateColumns();
        basic_variables = null;
        free_variables = null;
    }

    // Populates the columns lists based on what is in rows.
    // TODO definatly breaks with rows of differing length.
    public void UpdateColumns(){
        this.columns = new List<List<float>>();
        for(int i = 0 ; i < rows[0].Count ; i++){
            List<float> temp = new List<float>();
            foreach(List<float> row in rows){
                temp.Add(row[i]);
            }
            this.columns.Add(temp);
        }
    }

    // Returns the transpose of the matrix as a new matrix object.
    public Matrix Transpose(){
        return new Matrix(columns);
    }

    // Output a new matrix as the product of this matrix and the passed in scalar
    public Matrix sMultiply(float s){
        List<List<float>> matrix_body = new List<List<float>>();

        foreach(List<float> row in rows){
            List<float> temp = new List<float>();
            foreach(float t in row){
                temp.Add(t*s);
            }
            matrix_body.Add(temp);
        }

        return new Matrix(matrix_body);
    }

    // Output a new matrix as the product of two matrices.
    // this is the 'left' matrix and right_matrix is the 'right' matrix.
    public Matrix mMultiply(Matrix right_matrix){
        if(this.n == right_matrix.m){
            List<List<float>> matrix_body = new List<List<float>>();
            List<float> temp = new List<float>();
            float sum = 0;
    
            foreach(List<float> left_row in rows){
                foreach(List<float> right_column in right_matrix.columns){
                    sum = 0;
                    for(int i = 0 ; i < left_row.Count ; i++){
                        sum += left_row[i]*right_column[i];
                    }
                    temp.Add(sum);
                }
                matrix_body.Add(temp);
                temp = new List<float>();
            }
            return new Matrix(matrix_body);
        } else {
            // TODO Return an Identity Matrix or some such instead of null.
            Debug.Log ("Dimension Error: you cant multiply those two matrices like that.");
            return null;
        }
    }

    // Replace the row with the given index with the passed in row.
    public void RowReplace(int index, List<float> row){
        rows[index] = row;
        UpdateColumns();
    }

    // Exchange the two rows whose indices are passed int.
    public void RowExchange(int r1, int r2){
        List<float> temp = rows[r1];
        rows[r1] = rows[r2];
        rows[r2] = temp;
        UpdateColumns();
    }

    // Multiply the row with the given index by the passed in scalar.
    // Return a new row, the original row is unchanged.
    public List<float> RowMultiply(float scalar, int index){
        List<float> row = new List<float>();
        foreach(float f in rows[index]){
            row.Add(f*scalar);
        }
        return row;
    }

    // Add the passed in row to the row at given index.
    public void RowAdd(int index, List<float> row){
        if(rows[index].Count == row.Count){
            for(int i = 0 ; i < row.Count ; i++){
                rows[index][i] += row[i];
            }
        }
        UpdateColumns();
    }

    // Return a new matrix that is the reduced row echelon form of this matrix.
    // Note that I use the notation (j,k) to represent the value of the cell whose row is j and column is k.
    public Matrix rref(){
        Matrix reduced_matrix = new Matrix(this.rows);
        List<Vector2> pivots = new List<Vector2>();

        // ================  Guassian Elimination  ================
        // search across all of the columns
        for(int i = 0 ; i < reduced_matrix.n ; i ++){
            // down all of the rows
            for(int j = 0 ; j < reduced_matrix.m ; j++){
                // across the individual row
                for(int k = 0 ; k < reduced_matrix.n ; k++){
                    if(Mathf.Abs(reduced_matrix.rows[j][k]) > 0.00001){
                        if(k == i){
                            // (j,k) is a pivot
                            pivots.Add(new Vector2(j,k));
                            // down the individual column starting 1 below the pivot. reduce everything below to 0
                            for(int h = j+1 ; h < reduced_matrix.m ; h++){
                                // Add to row h (-1*(h,k)/(j,k))*(row j) so that cell (h,k) is 0 afterwards.
                                reduced_matrix.RowAdd(h, RowMultiply(-1.0f*(reduced_matrix.rows[h][k])/(reduced_matrix.rows[j][k]), j));
                                reduced_matrix.UpdateColumns();
                            }
                            // make the pivot positive if it was negative.
                            if(reduced_matrix.rows[j][k] < 0){
                                reduced_matrix.RowReplace(j, RowMultiply(-1.0f, j));
                                reduced_matrix.UpdateColumns();
                            }
                        }
                        break;
                    } else {
                        // This is so small that its just a rounding difference from 0, so make it 0
                        reduced_matrix.rows[j][k] = 0;
                    }
                }
            }
        }

        // rearrange the rows so that the pivots are in correct order.
        List<List<float>> temp = new List<List<float>>();
        for(int i = 0 ; i < pivots.Count ; i++){
            temp.Add(reduced_matrix.rows[(int)pivots[i].x]);
        }
        reduced_matrix = new Matrix(temp);
        // The pivot locations have changed now, so the old pivots are not needed.
        pivots.Clear();

        // ================  Back Subsitution  ================
        for(int i = 0 ; i < reduced_matrix.n ; i++){
            for(int k = 0 ; k < reduced_matrix.columns[i].Count ; k++){
                for(int h = 0 ; h < reduced_matrix.rows[k].Count ; h++){
                    // Find the first non zero element, this is a pivot - element(k,h) of the rows.
                    if(reduced_matrix.rows[k][h] != 0){
                        if(h == i){
                            // This is a pivot in the correct column! reduce everything above it to 0
                            pivots.Add(new Vector2(k,h));
                            for(int j = k-1 ; j >= 0 ; j--){
                                if(reduced_matrix.columns[i][j] != 0){
                                    reduced_matrix.RowAdd(j, reduced_matrix.RowMultiply((-1*reduced_matrix.columns[i][j])/(reduced_matrix.columns[h][k]) , k));
                                    reduced_matrix.UpdateColumns();
                                }
                            }
                        }
                        // break so we only get the first non zero value.
                        break;
                    }
                }
            }
        }

        //Normalize the diagonals to be 1
        List< List<float> > new_rows = new List<List<float>>();
        for(int i = 0 ; i < reduced_matrix.m ; i++){
            if(reduced_matrix.rows[i][i] != 0){
                new_rows.Add(reduced_matrix.RowMultiply( 1.0f/reduced_matrix.rows[i][i], i));
            } else {
                new_rows.Add(reduced_matrix.RowMultiply( 1.0f, i));
            }
        }
        reduced_matrix = new Matrix(new_rows);

        reduced_matrix.basic_variables = new List<int>();
        reduced_matrix.free_variables = new List<int>();
        // Populate the Free and Basic variable lists
        // Iterate accross the columns, if it has a pivot then that column is a free variable, otherwise its a basic variable.
        for(int i = 0 ; i < reduced_matrix.n ; i++){
            bool is_free = true;
            for(int j = 0 ; j < pivots.Count ; j++){
                if(pivots[j].y == i){
                    reduced_matrix.basic_variables.Add(i);
                    is_free = false;
                } 
            }
            if(is_free){
                reduced_matrix.free_variables.Add(i);
            }
        }

        return reduced_matrix;

    }

    // Display the matrix in [ ] notation.
    public override string ToString(){
        string mOut = "[";
        for(int j = 0; j < rows.Count; j++){
            for(int i = 0; i < rows[j].Count ; i++){
                mOut += "" + rows[j][i];
                // Only add the comma if it's not the last in the row.
                if(i != rows[j].Count -1){
                    mOut += ", ";
                }
            }
            mOut += "]";
            // Only add the new line if it's not the last row in the matrix.
            if(j != rows.Count - 1){
                mOut += "\r\n[";
            }
        }
        return mOut;
    }
}
