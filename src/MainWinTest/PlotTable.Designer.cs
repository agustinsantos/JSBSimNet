﻿namespace JSBSimTest
{
    partial class PlotTable
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.plotSurface = new NPlot.Windows.PlotSurface2D();
            this.SuspendLayout();
            // 
            // plotSurface
            // 
            this.plotSurface.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.plotSurface.AutoScaleAutoGeneratedAxes = false;
            this.plotSurface.AutoScaleTitle = false;
            this.plotSurface.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.plotSurface.DateTimeToolTip = false;
            this.plotSurface.Legend = null;
            this.plotSurface.LegendZOrder = -1;
            this.plotSurface.Location = new System.Drawing.Point(2, 0);
            this.plotSurface.Name = "plotSurface";
            this.plotSurface.RightMenu = null;
            this.plotSurface.ShowCoordinates = false;
            this.plotSurface.Size = new System.Drawing.Size(770, 360);
            this.plotSurface.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            this.plotSurface.TabIndex = 0;
            this.plotSurface.Text = "plotSurface";
            this.plotSurface.Title = "";
            this.plotSurface.TitleFont = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.plotSurface.XAxis1 = null;
            this.plotSurface.XAxis2 = null;
            this.plotSurface.YAxis1 = null;
            this.plotSurface.YAxis2 = null;
            // 
            // PlotTable
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(709, 347);
            this.Controls.Add(this.plotSurface);
            this.Name = "PlotTable";
            this.Text = "PlotTable";
            this.ResumeLayout(false);

        }

        #endregion

        private NPlot.Windows.PlotSurface2D plotSurface;
    }
}