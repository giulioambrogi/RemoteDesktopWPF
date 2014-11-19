using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookerClient
{
    
    class KeyboardMessage
    {
        int op;     
        RamGecTools.KeyboardHook.VKeys key ; 

        public KeyboardMessage(int op, RamGecTools.KeyboardHook.VKeys key){
            this.op = op; 
            this.key = key;
        }
    }
}
