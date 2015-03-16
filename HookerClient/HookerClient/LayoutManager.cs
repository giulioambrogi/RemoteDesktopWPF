using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HookerClient
{
    class LayoutManager
    {
        public LayoutManager() {}

        public void listViewItem(ListViewItem i)
        {
            
        }
        public void setComputerNameLabelLayout(Label l)
        {
            System.Windows.Media.BrushConverter bc = new System.Windows.Media.BrushConverter();
            //t.Background = (System.Windows.Media.Brush)bc.ConvertFromString("Yellow");
            l.Background = Brushes.Transparent;
           // l.VerticalAlignment = VerticalAlignment.Stretch;
            //l.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            l.Foreground = Brushes.White;
            //l.HorizontalAlignment = HorizontalAlignment.Stretch;
            //l.VerticalContentAlignment = VerticalAlignment.Stretch;
            l.FontSize = 30;

        }
        public void setPasswordTextBoxLayout(TextBox t){
            System.Windows.Media.BrushConverter bc = new System.Windows.Media.BrushConverter();
            //t.Background = (System.Windows.Media.Brush)bc.ConvertFromString("Yellow");
            t.Background = Brushes.Transparent;
            t.Height = 30;
            //t.Width = 150;
            t.VerticalAlignment = VerticalAlignment.Center;
            t.BorderBrush = Brushes.White;
            t.BorderThickness = new Thickness(0, 0, 1, 1);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            //t.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            t.Foreground = Brushes.White;
            t.HorizontalAlignment = HorizontalAlignment.Stretch;
            //t.Margin = new Thickness(5, 0, 5, 0);
            t.CaretBrush = Brushes.White;
            t.Cursor = System.Windows.Input.Cursors.Hand;
            //t.Text = "password";
            
        
        }

        public void setPortTextBoxLayout(TextBox t) {
            System.Windows.Media.BrushConverter bc = new System.Windows.Media.BrushConverter();
            //t.Background = (System.Windows.Media.Brush)bc.ConvertFromString("Yellow");
            t.Background = Brushes.Transparent;
            t.Height = 30;
            //t.Width = 150;
            t.VerticalAlignment = VerticalAlignment.Center;
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.BorderBrush = Brushes.White;
            t.BorderThickness = new Thickness(0, 0, 1, 1);
           //t.HorizontalContentAlignment = HorizontalAlignment.Center;
            t.Foreground = Brushes.White;
            t.HorizontalAlignment = HorizontalAlignment.Stretch;
            //t.Margin = new Thickness(5, 0, 5, 0);
            t.CaretBrush = Brushes.White;
            t.Cursor = System.Windows.Input.Cursors.Hand;
            //t.Text = "5143";
            
            
        }

        public void setCheckBoxLayout(CheckBox cb)
        {
            cb.VerticalAlignment = VerticalAlignment.Center;
            cb.HorizontalAlignment = HorizontalAlignment.Center;
            
            cb.BorderBrush = Brushes.White;
            cb.BorderThickness = new Thickness(2, 2, 2, 2);
            cb.Background = Brushes.Transparent; 
            
        }


        public void setGridLayout(Grid g)
        {
            
        }

        
        

    }
}
