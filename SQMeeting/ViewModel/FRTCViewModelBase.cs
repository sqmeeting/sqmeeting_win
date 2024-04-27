using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace SQMeeting.ViewModel
{
    public class FRTCViewModelBase : ViewModelBase
    {
        public FRTCViewModelBase()
        {
            this._dragMoveWndCommand = new RelayCommand<Window>((w) =>
            {
                w.DragMove();
            });

            this._closeWndCommand = new RelayCommand<Window>((w) =>
            {
                w.Close();
                if (ShutdownAfterClose)
                {
                    LogTool.LogHelper.Debug("Shutdown by wnd close, window is {0}", w.Name + w.Title + w.GetType().FullName);
                    (App.Current as App).RemoveMutex();
                    Environment.Exit(0);
                }
            });

            this._minimizeWndCommand = new RelayCommand<Window>((w) =>
            {
                w.WindowState = WindowState.Minimized;
            });
        }

        private bool _canMinimize = true;
        public bool CanMinimize
        {
            get { return _canMinimize; }
            set
            {
                _canMinimize = value;
                RaisePropertyChanged("CanMinimize");
            }
        }

        private bool _shutdownAfterClose = true;
        public bool ShutdownAfterClose
        {
            get { return _shutdownAfterClose; }
            set
            {
                _shutdownAfterClose = value;
                RaisePropertyChanged("ShutdownAfterClose");
            }
        }

        private bool _showMask = false;
        public bool ShowMask
        {
            get { return _showMask; }
            set { _showMask = value; RaisePropertyChanged("ShowMask"); }
        }

        private string _title = string.Empty;
        public string Title
        {
            get { return _title; }
            set { _title = value; RaisePropertyChanged("Title"); }
        }

        private Visibility _titleVisibility = Visibility.Visible;
        public Visibility TitleVisibility
        {
            get => _titleVisibility;
            set { _titleVisibility = value; RaisePropertyChanged(); }
        }

        private RelayCommand<Window> _dragMoveWndCommand;
        public RelayCommand<Window> DragMoveWndCommand
        {
            get
            {
                return _dragMoveWndCommand;
            }
        }

        private RelayCommand<Window> _closeWndCommand;
        public RelayCommand<Window> CloseWndCommand
        {
            get
            {
                return _closeWndCommand;
            }
        }

        private RelayCommand<Window> _minimizeWndCommand;
        public RelayCommand<Window> MinimizeWndCommand
        {
            get
            {
                return _minimizeWndCommand;
            }
        }
    }
}
