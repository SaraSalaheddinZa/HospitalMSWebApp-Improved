using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HospitalMSWebApp.Views.Laboratorian
{
    public partial class LaboratorianMaster : System.Web.UI.MasterPage
    {
        protected string ShellOpen(){return HospitalMSWebApp.Infrastructure.GuardianUi.MasterBody("Laboratory");}
        protected string ShellClose(){return HospitalMSWebApp.Infrastructure.GuardianUi.End();}
        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}
