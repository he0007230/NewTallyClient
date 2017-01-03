using System;
using System.Collections.Generic;
using System.Text;

namespace SYNCC
{
    public class SocketModel
    {
        public int type;
        public int area;
        public int command;
        public object message;

        public SocketModel() { }

        public SocketModel(int type, int area, int command, object message)
        {
            this.type = type;
            this.area = area;
            this.command = command;
            this.message = message;
        }

        public T getMessage<T>()
        {
            return (T)message;
        }
    }
}

