using System;
using System.Linq;
using System.Windows.Input;

namespace SQMeeting.Commands
{
    public class SQMeetingCommand<T1, T2> : ICommand
    {
        private Action<T1, T2> executeFunc;
        private Func<T1, T2, bool> canExecuteFunc;

        private bool _canExecute = true;

        public SQMeetingCommand(Action<T1, T2> Execute, Func<T1, T2, bool> CanExecute = null)
        {
            this.executeFunc = Execute;
            this.canExecuteFunc = CanExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if(canExecuteFunc != null)
            {
                object[] multiParam = (object[])parameter;
                bool ret = canExecuteFunc((T1)multiParam[0], (T2)multiParam[1]);
                if(_canExecute != ret)
                {
                    _canExecute = ret;
                    if(CanExecuteChanged != null)
                    {
                        CanExecuteChanged(this, new EventArgs());
                    }
                }
            }
            return _canExecute;
        }

        public void Execute(object parameter)
        {
            if (this.executeFunc != null)
            {
                object[] multiParam = (object[])parameter;
                if (multiParam != null && multiParam.Count() > 1)
                {
                    executeFunc((T1)multiParam[0], (T2)multiParam[1]);
                }
                else
                {
                    executeFunc(default(T1), default(T2));
                }
            }
        }
    }
}
