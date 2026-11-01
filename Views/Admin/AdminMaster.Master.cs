using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HospitalMSWebApp.Views.Admin
{
    public partial class AdminMaster : System.Web.UI.MasterPage
    {
        protected string ShellOpen(){return HospitalMSWebApp.Infrastructure.GuardianUi.MasterBody("Staff management");}
        protected string ShellClose(){return HospitalMSWebApp.Infrastructure.GuardianUi.End();}
        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}
