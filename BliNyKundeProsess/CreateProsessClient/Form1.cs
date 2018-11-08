using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Azure.WebJobs;

namespace CreateProsessClient
{
    public partial class Form1 : Form
    {
        [OrchestrationClient] DurableOrchestrationClient starter;
        public Form1()
        {
            InitializeComponent();
        }

        public DurableOrchestrationClient Starter { get => starter; set => starter = value; }

        private void btnCreateProcess_Click(object sender, EventArgs e)
        {
            var orchestrationId = Starter.StartNewAsync("O_BliNyKunde", "Firma001");
        }
    }
}
