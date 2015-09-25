using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace HookerClient
{
    class MyListViewItem
    {
         public MyListViewItem(Label PCName, TextBox PasswordTextBox, TextBox PortTextBox, CheckBox SelectionCheckBox)
        {
            this.PCName = PCName;
            this.PasswordTextBox = PasswordTextBox;
            this.PortTextBox = PortTextBox;
            this.SelectionCheckBox = SelectionCheckBox;
        }
         public Label PCName { get; set; }

         public TextBox PasswordTextBox  { get; set; }

         public TextBox PortTextBox { get; set; }
        public CheckBox SelectionCheckBox { get; set; }
    }
}
