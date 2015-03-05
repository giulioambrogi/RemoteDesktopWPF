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

        public void setComputerNameLabelLayout(Label l)
        {
            l.Height = 30;
            l.VerticalAlignment = VerticalAlignment.Center;
            System.Windows.Media.BrushConverter bc = new System.Windows.Media.BrushConverter();
            l.Background = (System.Windows.Media.Brush)bc.ConvertFromString("Red");
        }
        public void setPasswordTextBoxLayout(TextBox t){
            System.Windows.Media.BrushConverter bc = new System.Windows.Media.BrushConverter();
            t.Background = (System.Windows.Media.Brush)bc.ConvertFromString("Blue");
            t.Height = 30;
           /* Thickness thickness = new Thickness();
            thickness.Bottom = 1; thickness.Top = 1;
            t.BorderThickness = thickness;*/
           
            t.VerticalAlignment = VerticalAlignment.Center;
        
        }

        public void setPortTextBoxLayout(TextBox t) {
            System.Windows.Media.BrushConverter bc = new System.Windows.Media.BrushConverter();
            t.Background = (System.Windows.Media.Brush)bc.ConvertFromString("Yellow");
            t.Height = 30;
            t.VerticalAlignment = VerticalAlignment.Center;
        }

        public void setGridRowLayout(RowDefinition r) {
            GridLength h = new GridLength(30, GridUnitType.Star);
            r.Height = h;
            



        }

        public void setCheckBoxLayout(CheckBox cb)
        {
            cb.VerticalAlignment = VerticalAlignment.Center;
            
        }


        public void setGridLayout(Grid g)
        {
            
        }

        

    }
}
