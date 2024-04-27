using SQMeeting.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// UserSignInView.xaml 的交互逻辑
    /// </summary>
    public partial class UserSignInView : UserControl
    {
        public UserSignInView()
        {
            InitializeComponent();
            IsVisibleChanged += UserSignInView_IsVisibleChanged;
        }

        private void UserSignInView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            string remember = ConfigurationManager.AppSettings["RememberSignInPWD"];
            if(!string.IsNullOrEmpty(remember))
            {
                bool bRemember = false;
                bool.TryParse(remember, out bRemember);
                if (!(bool)e.NewValue)
                {
                    if(!bRemember)
                    {
                        this.pwdBox.Password = "";
                    }
                }
                else if (bRemember && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["SignInPWD"]))
                {
                    ViewModel.FRTCUserViewModel vm = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCUserViewModel>();
                    byte[] saved = FRTCUIUtils.AESDecrypt(Convert.FromBase64String(ConfigurationManager.AppSettings["SignInPWD"]), "ELPsyCongroo");
                    XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(saved, XmlDictionaryReaderQuotas.Max);
                    string server = string.Empty;
                    string name = string.Empty;
                    string pwd = string.Empty;
                    bool findServer = false;
                    bool findName = false;
                    bool findPwd = false;
                    try
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "server")
                            {
                                findServer = true;
                            }
                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "name")
                            {
                                findName = true;
                            }
                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "pwd")
                            {
                                findPwd = true;
                            }

                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "server")
                            {
                                findServer = false;
                            }
                            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "name")
                            {
                                findName = false;
                            }
                            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "pwd")
                            {
                                findPwd = false;
                            }

                            if (findServer && reader.NodeType == XmlNodeType.Text)
                            {
                                server = reader.Value;
                            }
                            else if (findName && reader.NodeType == XmlNodeType.Text)
                            {
                                name = reader.Value;
                            }
                            else if (findPwd && reader.NodeType == XmlNodeType.Text)
                            {
                                pwd = reader.Value;
                            }
                        }
                    }
                    catch { }

                    if (vm.ServerAddress == server && vm.UserName == name)
                    {
                        this.pwdBox.Password = pwd;
                    }
                    else
                    {
                        this.pwdBox.Clear();
                    }
                    reader.Close();
                }
            }           
        }
    }
}
