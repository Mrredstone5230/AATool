﻿using AATool.Data;
using AATool.Graphics;
using AATool.Net;
using AATool.Settings;
using AATool.UI.Screens;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace AATool.UI.Controls
{
    class UIAchievementTree : UIGrid
    {
        private List<UIAdvancement> achievements;
        private List<List<Cell>> paths;
        private bool[,] occupied;


        public UIAchievementTree()
        {
            this.DrawMode = DrawMode.All;
        }

        public override void InitializeRecursive(UIScreen screen)
        {
            base.InitializeRecursive(screen);
            this.achievements = new List<UIAdvancement>();
            this.occupied = new bool[this.Rows.Count, this.Columns.Count];
            foreach (UIControl control in this.Children)
            {
                if (control is UIAdvancement achievement)
                {
                    achievement.First<UITextBlock>().DrawBackground = true;
                    this.achievements.Add(achievement);
                }
                this.occupied[control.Row, control.Column] = true;
            }
        }

        public override void ResizeRecursive(Rectangle rectangle)
        {
            base.ResizeRecursive(rectangle);
            this.GeneratePaths();
        }

        private void GeneratePaths()
        {
            this.paths = new List<List<Cell>>();
            if (Peer.IsRunning)
                return;

            foreach (UIAdvancement achievement in this.achievements)
            {
                //iterate each direct descendant and construct an arrow to it
                foreach (string key in (achievement.Advancement as Achievement)?.Children.Keys)
                {
                    UIControl control = this.First(key);
                    List<Cell> path = this.FindPathOf(new Cell(achievement.Row, achievement.Column), new Cell(control.Row, control.Column));
                    this.paths.Add(path);
                }
            }
        }

        //breadth first search to find shortest, unobstructed path between two points
        public List<Cell> FindPathOf(Cell start, Cell destination)
        {
            var visited = new HashSet<Cell>();
            var paths   = new Queue<List<Cell>>();

            //begin search at start point
            paths.Enqueue(new List<Cell>() { start });

            while (paths.Count > 0)
            {
                var path = paths.Dequeue();
                var node = path.Last();

                //iterate adjacent cells and check if they're what we're looking for
                //if they're not, check if they're empty and queue a new path
                foreach (var neighbor in this.GetNeighboringCells(node.Row, node.Column, path.Count > 1 ? path[path.Count - 2] : node))
                {
                    if (neighbor.Equals(destination))
                    {
                        //destination found! return path taken to get there
                        path.Add(neighbor);
                        return path;
                    }
                    else if (!visited.Contains(new Cell(neighbor.Row, neighbor.Column)))
                    {
                        //cell isn't empty and isn't destination; this path is at an end
                        if (this.occupied[neighbor.Row, neighbor.Column])
                            continue;

                        //add neighbor to path and add path to queue
                        visited.Add(neighbor);
                        var newPath = new List<Cell>(path);
                        newPath.Add(neighbor);
                        paths.Enqueue(newPath);
                    }
                }
            }
            return null;
        }

        public List<Cell> GetNeighboringCells(int row, int col, Cell previous)
        {
            //perform bounds check and return adjacent cells
            var neighbors = new List<Cell>();

            Rectangle cell = this.CellRectangles[previous.Row, previous.Column];
            bool canTurn = cell.Width is 64 && cell.Height is 64;

            //prefer current direction to find path with fewest angle changes
            if (previous.Column == col)
            {
                if (row + 1 < this.Rows.Count)
                    neighbors.Add(new Cell(row + 1, col));
                if (row - 1 >= 0)
                    neighbors.Add(new Cell(row - 1, col));
                if (col + 1 < this.Columns.Count)
                    neighbors.Add(new Cell(row, col + 1));
                if (col - 1 >= 0)
                    neighbors.Add(new Cell(row, col - 1));
            }
            else
            {
                if (col + 1 < this.Columns.Count)
                    neighbors.Add(new Cell(row, col + 1));
                if (col - 1 >= 0)
                    neighbors.Add(new Cell(row, col - 1));
                if (row + 1 < this.Rows.Count)
                    neighbors.Add(new Cell(row + 1, col));
                if (row - 1 >= 0)
                    neighbors.Add(new Cell(row - 1, col));
            }          
            return neighbors;
        }

        public override void DrawThis(Display display)
        {
            display.DrawRectangle(this.Bounds, MainSettings.Instance.BackColor, MainSettings.Instance.BorderColor, 1);

            int thickness = 8;
            int halfThickness = thickness / 2;
            foreach (List<Cell> path in this.paths)
            {
                //calculate arrow color based on completion of parent achievement
                var start = this.CellContents[path.First().Row, path.First().Column] as UIAdvancement;
                double blend = (start.Advancement.CompletedByAnyone()) ? 2 : 0.5;
                byte r   = (byte)((MainSettings.Instance.BorderColor.R * blend) + MainSettings.Instance.BackColor.R * (1 - blend));
                byte g   = (byte)((MainSettings.Instance.BorderColor.G * blend) + MainSettings.Instance.BackColor.G * (1 - blend));
                byte b   = (byte)((MainSettings.Instance.BorderColor.B * blend) + MainSettings.Instance.BackColor.B * (1 - blend));
                var tint = Color.FromNonPremultiplied(r, g, b, 255);

                for (int i = 1; i < path.Count - 1; i++)
                {
                    Cell current = path[i];
                    Cell prev    = path[i - 1];
                    Cell next    = path[i + 1];
                    Rectangle bounds = this.CellRectangles[current.Row, current.Column];

                    //get connection states
                    bool connectedLeft   = current.Column > prev.Column || current.Column > next.Column;
                    bool connectedRight  = current.Column < prev.Column || current.Column < next.Column;
                    bool connectedTop    = current.Row    > prev.Row    || current.Row    > next.Row;
                    bool connectedBottom = current.Row    < prev.Row    || current.Row    < next.Row;

                    //draw each segment of the line depending on adjacent segments
                    if (connectedLeft)
                        display.DrawRectangle(new Rectangle(new Point(bounds.Center.X - bounds.Width / 2, bounds.Center.Y - halfThickness), new Point(bounds.Width / 2, thickness)), tint);
                    if (connectedRight)
                        display.DrawRectangle(new Rectangle(new Point(bounds.Center.X, bounds.Center.Y - halfThickness), new Point(bounds.Width / 2, thickness)), tint);
                    if (connectedTop)
                        display.DrawRectangle(new Rectangle(new Point(bounds.Center.X - halfThickness, bounds.Center.Y - bounds.Height / 2), new Point(thickness, bounds.Height / 2)), tint);              
                    if (connectedBottom)
                        display.DrawRectangle(new Rectangle(new Point(bounds.Center.X - halfThickness, bounds.Center.Y), new Point(thickness, bounds.Height / 2)), tint);

                    //fill center of intersection
                    if ((connectedLeft || connectedRight) && (connectedTop || connectedBottom))
                        display.DrawRectangle(new Rectangle(new Point(bounds.Center.X - halfThickness, bounds.Center.Y - halfThickness), new Point(thickness, thickness)), tint);

                    //if this is the last segment before the next achievement, draw arrow head
                    if (i == path.Count - 2)
                    {
                        string direction = "";
                        if (current.Column > next.Column)
                            direction = "left";
                        if (current.Column < next.Column)
                            direction = "right";
                        if (current.Row > next.Row)
                            direction = "up";
                        if (current.Row < next.Row)
                            direction = "down";

                        string mode = (bounds.Width < 64 || bounds.Height < 64) ? "compact" : "relaxed";
                        display.Draw($"arrow_{mode}_bg_{direction}", bounds, MainSettings.Instance.BackColor);
                        display.Draw($"arrow_{mode}_head_{direction}", bounds, tint);
                    }                    
                }
            } 
        }
    }
}
