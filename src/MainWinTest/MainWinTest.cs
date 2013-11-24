#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///  
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU General Public License for more details.
///  
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
#endregion

namespace JSBSimTest
{
	using System;
	using System.Drawing;
	using System.Collections;
	using System.ComponentModel;
	using System.Windows.Forms;
	using System.Data;
	using System.Runtime.Serialization;
	using System.IO;
    using System.Xml;
    using System.Text;
	using System.Runtime.Serialization.Formatters.Soap;
	using System.Runtime.Serialization.Formatters.Binary;
    using System.Text.RegularExpressions;



	// Import log4net classes.
	using log4net;

	using JSBSim;
    using JSBSim.Script;
    using JSBSim.MathValues;

	using CommonUtils.MathLib;

    using JSBSim.Tests;

	/// <summary>
	/// Summary description for MainTest.
	/// </summary>
	public class MainWinTest : System.Windows.Forms.Form
	{

        /// <summary>
        /// Define a static logger variable so that it references the
        ///	Logger instance.
        /// 
        /// NOTE that using System.Reflection.MethodBase.GetCurrentMethod().DeclaringType
        /// is equivalent to typeof(LoggingExample) but is more portable
        /// i.e. you can copy the code directly into another class without
        /// needing to edit the code.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private MenuStrip menuStrip1;
        private ToolStripMenuItem testToolStripMenuItem;
        private ToolStripMenuItem testLoadScriptToolStripMenuItem;
        private ToolStripMenuItem loadC1723ToolStripMenuItem;
        private ToolStripMenuItem loadBallToolStripMenuItem;
        private ToolStripMenuItem loadScriptToolStripMenuItem;
        private OpenFileDialog openFileDialog1;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem initialConditionsToolStripMenuItem;
        private ToolStripMenuItem simpleTestToolStripMenuItem;
        private ToolStripMenuItem runModelToolStripMenuItem;
        private ToolStripMenuItem runMk82ToolStripMenuItem;
        private ToolStripMenuItem runBallToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem runModelToolStripMenuItem1;
        private ToolStripMenuItem runMk82ToolStripMenuItem1;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem loadRunScriptToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem runBallToolStripMenuItem1;
        private ToolStripMenuItem nunitToolStripMenuItem;
        private ToolStripMenuItem runTestToolStripMenuItem;
        private ToolStripMenuItem loadTestToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem plotToolStripMenuItem;
        private ToolStripMenuItem tableToolStripMenuItem;
        private ToolStripMenuItem testToolStripMenuItem1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem closeToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private RichTextBox aircraftEditor;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public MainWinTest()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            FileInfo logFile = new System.IO.FileInfo("Log4Net.config");
            if (logFile.Exists)
            {
                // Log4Net is configured using a DOMConfigurator.
                log4net.Config.XmlConfigurator.Configure(logFile);
            }
            else
            {
                // Set up a simple configuration that logs on the console.
                log4net.Config.BasicConfigurator.Configure();
            }


			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.testLoadScriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadC1723ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadBallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.loadScriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.runMk82ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.runBallToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.loadRunScriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.initialConditionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.simpleTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runModelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runMk82ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runBallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.runModelToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.nunitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.plotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.testToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.aircraftEditor = new System.Windows.Forms.RichTextBox();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.plotToolStripMenuItem,
            this.testToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(568, 26);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // testToolStripMenuItem
            // 
            this.testToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.testLoadScriptToolStripMenuItem,
            this.initialConditionsToolStripMenuItem,
            this.runModelToolStripMenuItem,
            this.nunitToolStripMenuItem});
            this.testToolStripMenuItem.Name = "testToolStripMenuItem";
            this.testToolStripMenuItem.Size = new System.Drawing.Size(50, 22);
            this.testToolStripMenuItem.Text = "Test";
            // 
            // testLoadScriptToolStripMenuItem
            // 
            this.testLoadScriptToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadC1723ToolStripMenuItem,
            this.loadBallToolStripMenuItem,
            this.toolStripSeparator4,
            this.loadScriptToolStripMenuItem,
            this.toolStripSeparator3,
            this.runMk82ToolStripMenuItem1,
            this.runBallToolStripMenuItem1,
            this.toolStripSeparator1,
            this.loadRunScriptToolStripMenuItem});
            this.testLoadScriptToolStripMenuItem.Name = "testLoadScriptToolStripMenuItem";
            this.testLoadScriptToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.testLoadScriptToolStripMenuItem.Text = "Scripts";
            // 
            // loadC1723ToolStripMenuItem
            // 
            this.loadC1723ToolStripMenuItem.Name = "loadC1723ToolStripMenuItem";
            this.loadC1723ToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.loadC1723ToolStripMenuItem.Text = "Load C1723";
            this.loadC1723ToolStripMenuItem.Click += new System.EventHandler(this.LoadScriptC1723);
            // 
            // loadBallToolStripMenuItem
            // 
            this.loadBallToolStripMenuItem.Name = "loadBallToolStripMenuItem";
            this.loadBallToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.loadBallToolStripMenuItem.Text = "Load Ball";
            this.loadBallToolStripMenuItem.Click += new System.EventHandler(this.LoadScripBall);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(164, 6);
            // 
            // loadScriptToolStripMenuItem
            // 
            this.loadScriptToolStripMenuItem.Name = "loadScriptToolStripMenuItem";
            this.loadScriptToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.loadScriptToolStripMenuItem.Text = "Load Script ...";
            this.loadScriptToolStripMenuItem.Click += new System.EventHandler(this.LoadScripFromFile);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(164, 6);
            // 
            // runMk82ToolStripMenuItem1
            // 
            this.runMk82ToolStripMenuItem1.Name = "runMk82ToolStripMenuItem1";
            this.runMk82ToolStripMenuItem1.Size = new System.Drawing.Size(167, 22);
            this.runMk82ToolStripMenuItem1.Text = "Run Mk82";
            this.runMk82ToolStripMenuItem1.Click += new System.EventHandler(this.RunScriptMk82);
            // 
            // runBallToolStripMenuItem1
            // 
            this.runBallToolStripMenuItem1.Name = "runBallToolStripMenuItem1";
            this.runBallToolStripMenuItem1.Size = new System.Drawing.Size(167, 22);
            this.runBallToolStripMenuItem1.Text = "Run Ball";
            this.runBallToolStripMenuItem1.Click += new System.EventHandler(this.RunScriptBall);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(164, 6);
            // 
            // loadRunScriptToolStripMenuItem
            // 
            this.loadRunScriptToolStripMenuItem.Name = "loadRunScriptToolStripMenuItem";
            this.loadRunScriptToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.loadRunScriptToolStripMenuItem.Text = "Run Script ...";
            this.loadRunScriptToolStripMenuItem.Click += new System.EventHandler(this.RunScriptFromFile);
            // 
            // initialConditionsToolStripMenuItem
            // 
            this.initialConditionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.simpleTestToolStripMenuItem});
            this.initialConditionsToolStripMenuItem.Name = "initialConditionsToolStripMenuItem";
            this.initialConditionsToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.initialConditionsToolStripMenuItem.Text = "Initial Conditions";
            // 
            // simpleTestToolStripMenuItem
            // 
            this.simpleTestToolStripMenuItem.Name = "simpleTestToolStripMenuItem";
            this.simpleTestToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.simpleTestToolStripMenuItem.Text = "Simple Test";
            this.simpleTestToolStripMenuItem.Click += new System.EventHandler(this.TestICProperties_Click);
            // 
            // runModelToolStripMenuItem
            // 
            this.runModelToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runMk82ToolStripMenuItem,
            this.runBallToolStripMenuItem,
            this.toolStripSeparator2,
            this.runModelToolStripMenuItem1});
            this.runModelToolStripMenuItem.Name = "runModelToolStripMenuItem";
            this.runModelToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.runModelToolStripMenuItem.Text = "Run Model";
            this.runModelToolStripMenuItem.Click += new System.EventHandler(this.RunModelFromFile_Click);
            // 
            // runMk82ToolStripMenuItem
            // 
            this.runMk82ToolStripMenuItem.Name = "runMk82ToolStripMenuItem";
            this.runMk82ToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.runMk82ToolStripMenuItem.Text = "Run Mk82";
            this.runMk82ToolStripMenuItem.Click += new System.EventHandler(this.RunModelMk82_Click);
            // 
            // runBallToolStripMenuItem
            // 
            this.runBallToolStripMenuItem.Name = "runBallToolStripMenuItem";
            this.runBallToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.runBallToolStripMenuItem.Text = "Run Ball";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(161, 6);
            // 
            // runModelToolStripMenuItem1
            // 
            this.runModelToolStripMenuItem1.Name = "runModelToolStripMenuItem1";
            this.runModelToolStripMenuItem1.Size = new System.Drawing.Size(164, 22);
            this.runModelToolStripMenuItem1.Text = "Run Model ...";
            this.runModelToolStripMenuItem1.Click += new System.EventHandler(this.RunModelFromFile_Click);
            // 
            // nunitToolStripMenuItem
            // 
            this.nunitToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runTestToolStripMenuItem,
            this.loadTestToolStripMenuItem});
            this.nunitToolStripMenuItem.Name = "nunitToolStripMenuItem";
            this.nunitToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.nunitToolStripMenuItem.Text = "Nunit";
            // 
            // runTestToolStripMenuItem
            // 
            this.runTestToolStripMenuItem.Name = "runTestToolStripMenuItem";
            this.runTestToolStripMenuItem.Size = new System.Drawing.Size(137, 22);
            this.runTestToolStripMenuItem.Text = "RunTest";
            this.runTestToolStripMenuItem.Click += new System.EventHandler(this.ExecuteRunTest);
            // 
            // loadTestToolStripMenuItem
            // 
            this.loadTestToolStripMenuItem.Name = "loadTestToolStripMenuItem";
            this.loadTestToolStripMenuItem.Size = new System.Drawing.Size(137, 22);
            this.loadTestToolStripMenuItem.Text = "LoadTest";
            this.loadTestToolStripMenuItem.Click += new System.EventHandler(this.ExecuteLoadTests);
            // 
            // plotToolStripMenuItem
            // 
            this.plotToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tableToolStripMenuItem,
            this.testToolStripMenuItem1});
            this.plotToolStripMenuItem.Name = "plotToolStripMenuItem";
            this.plotToolStripMenuItem.Size = new System.Drawing.Size(43, 22);
            this.plotToolStripMenuItem.Text = "Plot";
            // 
            // tableToolStripMenuItem
            // 
            this.tableToolStripMenuItem.Name = "tableToolStripMenuItem";
            this.tableToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.tableToolStripMenuItem.Text = "Table";
            this.tableToolStripMenuItem.Click += new System.EventHandler(this.PlotTable);
            // 
            // testToolStripMenuItem1
            // 
            this.testToolStripMenuItem1.Name = "testToolStripMenuItem1";
            this.testToolStripMenuItem1.Size = new System.Drawing.Size(112, 22);
            this.testToolStripMenuItem1.Text = "Test";
            this.testToolStripMenuItem1.Click += new System.EventHandler(this.PlotTest);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(58, 22);
            this.aboutToolStripMenuItem.Text = "About";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // aircraftEditor
            // 
            this.aircraftEditor.Location = new System.Drawing.Point(0, 29);
            this.aircraftEditor.Name = "aircraftEditor";
            this.aircraftEditor.Size = new System.Drawing.Size(568, 387);
            this.aircraftEditor.TabIndex = 3;
            this.aircraftEditor.Text = "";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(40, 22);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenAircraft);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.closeToolStripMenuItem.Text = "Close";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveToolStripMenuItem.Text = "Save";
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveAsToolStripMenuItem.Text = "Save as...";
            // 
            // MainWinTest
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
            this.ClientSize = new System.Drawing.Size(568, 405);
            this.Controls.Add(this.aircraftEditor);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWinTest";
            this.Text = "JSBSim Tests";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

        private const String aircraft = "737.xml";
        private const String aircraft_MK82 = "mk82";
        private const String aircraft_Ball = "ball";
        private string rootDirectory = "./Models";
        private string dialogDirectory = "./Models";


		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new MainWinTest());
		}


        private void TestICProperties_Click(object sender, System.EventArgs e)
        {
            string testIC =
                @"<?xml version=""1.0""?>
                    <initialize name=""reset00"">
                      <!--
                        This file sets up the mk82 to start off
                        from altitude.
                       <latitude unit=""DEG"">   3.0  </latitude>
                      <longitude unit=""DEG"">  7.0  </longitude>
                      <altitude unit=""FT"">    9.0  </altitude>
                     -->
                    <ubody unit=""FT/SEC"">  400.0  </ubody>
                    <vbody unit=""FT/SEC"">    0.0  </vbody>
                    <wbody unit=""FT/SEC"">  120.0  </wbody>
                    </initialize>";

            string testProperties =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function NAME=""aero/coefficient/ClDf2"">
                        <sum>
                          <property>ic/lat-gc-deg</property>
                          <property>ic/lat-gc-deg</property>
                          <property>ic/lat-gc-deg</property>
                        </sum>
                    </function>";

            FDMExecutive fdm = new FDMExecutive();

            XmlElement elemIc = BuildXmlConfig(testIC, "initialize");
            InitialCondition IC = fdm.GetIC;
            IC.Load(elemIc);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim IC InputOutput: Lat., Lon., Alt.");
            }

            XmlElement elemFunction = BuildXmlConfig(testProperties, "function");
            JSBSim.MathValues.Function func = new JSBSim.MathValues.Function(fdm.PropertyManager, elemFunction);

            //Checks InputOutput 
            log.Debug(" The value =" + IC.LatitudeDegIC + IC.LongitudeDegIC + IC.AltitudeFtIC + ", the func =" + func.GetValue());
        }


        private XmlElement BuildXmlConfig(string config, string tag)
        {
            XmlDocument doc = new XmlDocument();
            Stream configStream = new MemoryStream(Encoding.Unicode.GetBytes(config));
            // Create a validating reader arround a text reader for the file stream
            XmlReader xmlReader = new XmlTextReader(configStream);

            // load the data into the dom
            doc.Load(xmlReader);
            XmlNodeList childNodes = doc.GetElementsByTagName(tag);

            return childNodes[0] as XmlElement;
        }

        private void LoadScript(string filename)
        {
            FDMExecutive fdm = new FDMExecutive();
            fdm.AircraftPath = rootDirectory + "/aircraft";
            fdm.EnginePath = rootDirectory + "/engine";

            Script script = new Script(fdm);
            script.LoadScript(filename);
        }

        private void LoadScriptC1723(object sender, EventArgs e)
        {
            LoadScript("../../../Models/scripts/c1723.xml");
        }

        private void LoadScripBall(object sender, EventArgs e)
        {
            LoadScript(rootDirectory + "/scripts/ball.xml");
        }

        private void LoadScripFromFile(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = dialogDirectory + "/scripts";
            openFileDialog1.Filter = "XMl files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Title = "Select a script file to load...";

            string scriptFilename;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((scriptFilename = openFileDialog1.FileName) != null)
                {
                   LoadScript(scriptFilename);
                }
            }

        }

        private void RunModelFromFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = dialogDirectory + "/aircraft";
            openFileDialog1.Filter = "XMl files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Title = "Select a Model file to load...";

            string scriptFilename;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((scriptFilename = openFileDialog1.FileName) != null)
                {
                    LoadAndRunModel(scriptFilename);
                }
            }
        }

        private void LoadAndRunModel(string modelFileName)
        { 
            FDMExecutive fdm = new FDMExecutive();
            fdm.AircraftPath = rootDirectory + "/aircraft";
            fdm.EnginePath = rootDirectory + "/engine";

            fdm.LoadModel(modelFileName, true);

            InitialCondition IC = fdm.GetIC;
            IC.Load("reset00", true);

            Trim fgt = new Trim(fdm, TrimMode.Full);
            if ( !fgt.DoTrim() ) {
              log.Debug("Trim Failed");
            }
            fgt.Report();

            bool result = fdm.Run();
            int count = 0;
            while (result && !(fdm.Holding() || fdm.State.IsIntegrationSuspended))
            {
                result = fdm.Run();
                count++;
                if (count > 10 && log.IsDebugEnabled)
                {
                    count = 0;
                    log.Debug("=> Time: " + fdm.State.SimTime);
                }
            }
        }

        private void RunModelMk82_Click(object sender, System.EventArgs e)
        {
            LoadAndRunModel(aircraft_MK82);
        }

        private void RunScriptFromFile(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = dialogDirectory + "/scripts";
            openFileDialog1.Filter = "XMl files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Title = "Select a script file to load...";

            string scriptFilename;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((scriptFilename = openFileDialog1.FileName) != null)
                {
                    LoadAndRunScript(scriptFilename);
                }
            }

        }
        private void LoadAndRunScript(string scriptFileName)
        { 
            FDMExecutive fdm = new FDMExecutive();
            fdm.AircraftPath = "../aircraft";
            fdm.EnginePath =  "../engine";

            Script script = new Script(fdm);
            script.LoadScript(scriptFileName);

            bool result = fdm.Run();
            bool scriptResult = true;
            int count = 0;
            while (result && scriptResult && !(fdm.Holding() || fdm.State.IsIntegrationSuspended))
            {
                scriptResult = script.RunScript();
                if (!scriptResult)
                    break;
                result = fdm.Run();
                count++;
                if (count > 10 && log.IsDebugEnabled)
                {
                    count = 0;
                    log.Debug("=> Time: " + fdm.State.SimTime);
                }
            }
        }

        private void RunScriptMk82(object sender, EventArgs e)
        {
            LoadAndRunScript(rootDirectory + "/scripts/" + aircraft_MK82+".xml");
        }

        private void RunScriptBall(object sender, EventArgs e)
        {

            LoadAndRunScript(rootDirectory + "/scripts/ballCSharp.xml");
        }

        private void ExecuteLoadTests(object sender, EventArgs e)
        {
            LoadTests test = new LoadTests();

            test.CheckLoad_C172x();
            test.CheckLoad_737();
            test.CheckLoad_747();
            test.CheckLoad_MK82();
            test.CheckLoad_f16();
            test.CheckLoad_p51d();
            test.CheckLoad2_MK82();
            test.CheckLoad_Ball();
            test.CheckLoad_A320();
        }

        private void ExecuteRunTest(object sender, EventArgs e)
        {
            RunTests test = new RunTests();

            test.CheckRun_MK82();
            test.CheckRun_Ball();

        }

        private PlotTable displayForm = null;
        private void PlotTable(object sender, EventArgs e)
        {
            string test =
               @"<?xml version=""1.0""?>
				  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
				  <table>
                          <independentVar>aero/alpha-rad</independentVar>
                          <tableData>
                              -0.2000	-0.6800	
                              0.0000	0.1000	
                              0.0100	1.0000	
                              0.1500	1.5000	
                              0.2000	1.7000	
                              0.2300	1.4000	
                              0.3000	1.0000
                              0.4000	0.9000
                              0.5000	0.7000
                              0.6000	0.6000
                          </tableData>
				</table>";
            JSBSim.InputOutput.PropertyManager propMngr = new JSBSim.InputOutput.PropertyManager();
            ClassWithPropertiesForTables class1 = new ClassWithPropertiesForTables("", propMngr);

            XmlElement elem = BuildXmlConfig(test);
            Table table = new Table(propMngr, elem);

            displayForm = new PlotTable();
            displayForm.Plot(table);
            displayForm.ShowDialog();
        }

        private void PlotTest(object sender, EventArgs e)
        {
            displayForm = new PlotTable();
            displayForm.PlotTest();
            displayForm.ShowDialog();
        }

        private XmlElement BuildXmlConfig(string config)
        {
            XmlDocument doc = new XmlDocument();
            Stream configStream = new MemoryStream(Encoding.Unicode.GetBytes(config));

            XmlReader xmlReader = new XmlTextReader(configStream);
            // load the data into the dom
            doc.Load(xmlReader);
            xmlReader.Close();

            XmlNodeList childNodes = doc.GetElementsByTagName("table");

            return childNodes[0] as XmlElement;
        }

        private void OpenAircraft(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = dialogDirectory + "/aircraft";
            openFileDialog1.Filter = "XMl files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Title = "Select an aircraft file to load...";

            string scriptFilename;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((scriptFilename = openFileDialog1.FileName) != null)
                {
                    FDMExecutive fdm = new FDMExecutive();
                    fdm.AircraftPath = rootDirectory + "/aircraft";
                    fdm.EnginePath = rootDirectory + "/engine";

                    //fdm.LoadModel(modelFileName, true);


                }
            }
        }

	}

    public class ClassWithPropertiesForTables
    {
        public ClassWithPropertiesForTables(string path, JSBSim.InputOutput.PropertyManager propMngr)
        {
            propMngr.Bind(path, this);
        }

        [ScriptAttribute("aero/alpha-rad", "A test property")]
        public double AlphaRad
        {
            get { return this.alphaRad; }
            set { this.alphaRad = value; }
        }

        [ScriptAttribute("fcs/flap-pos-deg", "A test property")]
        public double FlapPosDeg
        {
            get { return this.flapPosDeg; }
            set { this.flapPosDeg = value; }
        }


        public double alphaRad = 1.0;
        public double flapPosDeg = 1.0;
    }

}
