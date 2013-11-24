using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using NPlot;

using JSBSim.MathValues;
namespace JSBSimTest
{
    public partial class PlotTable : Form
    {
        public PlotTable()
        {
            InitializeComponent();

            plotSurface.RightMenu = NPlot.Windows.PlotSurface2D.DefaultContextMenu;
        }

        public void Plot(Table table)
        {
            plotSurface.Clear();

            // draw a fine grid. 
            Grid fineGrid = new Grid();
            fineGrid.VerticalGridType = Grid.GridType.Fine;
            fineGrid.HorizontalGridType = Grid.GridType.Fine;
            plotSurface.Add(fineGrid);

            //plotSurface.AddInteraction(new NPlot.Windows.PlotSurface2D.Interactions.VerticalGuideline());
            plotSurface.AddInteraction(new NPlot.Windows.PlotSurface2D.Interactions.HorizontalRangeSelection());
            plotSurface.AddInteraction(new NPlot.Windows.PlotSurface2D.Interactions.AxisDrag(true));

            plotSurface.Add(new HorizontalLine(0.0, Color.LightBlue));

            System.Double[] data = new Double[200];
            System.Double[] axis = new Double[200];

            double step = (table.GetUpperKey(1) - table.GetLowerKey(1))/200.0;
            int i = 0;
            for (double key = table.GetLowerKey(1); key <= table.GetUpperKey(1); key += step)
            {
                data[i] = table.GetValue(key);
                axis[i] = key;
                i++;
            }

            LinePlot lp = new LinePlot();
            lp.OrdinateData = data;
            lp.AbscissaData = axis;
            lp.Pen = new Pen(Color.Red);
            plotSurface.Add(lp);

            plotSurface.YAxis1.FlipTicksLabel = true;

            plotSurface.Refresh();

        }

        public void PlotTest()
        {
            plotSurface.Clear();

            // can plot different types.
            ArrayList l = new ArrayList();
            l.Add((int)2);
            l.Add((double)1.0);
            l.Add((float)3.0);
            l.Add((int)5.0);

            LinePlot lp1 = new LinePlot(new double[] { 4.0, 3.0, 5.0, 8.0 });
            lp1.Pen = new Pen(Color.LightBlue);
            lp1.Pen.Width = 2.0f;

            //lp.AbscissaData = new StartStep( 0.0, 2.0 );

            LinePlot lp2 = new LinePlot(new double[] { 2.0, 1.0, 4.0, 5.0 });
            lp2.Pen = new Pen(Color.LightBlue);
            lp2.Pen.Width = 2.0f;

            FilledRegion fr = new FilledRegion(lp1, lp2);

            plotSurface.Add(fr);

            plotSurface.Add(new Grid());
            plotSurface.Add(lp1);
            plotSurface.Add(lp2);

            ArrowItem a = new ArrowItem(new PointD(2, 4), -50.0f, "Arrow");
            a.HeadOffset = 5;
            a.ArrowColor = Color.Red;
            a.TextColor = Color.Purple;
            plotSurface.Add(a);

            MarkerItem m = new MarkerItem(new Marker(Marker.MarkerType.TriangleDown, 8, Color.ForestGreen), 1.38, 2.9);
            plotSurface.Add(m);

            plotSurface.XAxis1.TicksCrossAxis = true;

            ((LinearAxis)plotSurface.XAxis1).LargeTickValue = -4.1;
            ((LinearAxis)plotSurface.XAxis1).AutoScaleText = true;
            ((LinearAxis)plotSurface.XAxis1).TicksIndependentOfPhysicalExtent = true;
            //plotSurface.XAxis1.Label = "Hello world";

            plotSurface.Refresh();
        }
    }
}