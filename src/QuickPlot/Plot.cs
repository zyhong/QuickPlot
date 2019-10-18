﻿using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace QuickPlot
{
    public class Plot
    {
        public PlotSettings.SubplotPosition subplotPosition = new PlotSettings.SubplotPosition(1, 1, 1);
        private List<Plottables.Plottable> plottables = new List<Plottables.Plottable>();
        public readonly PlotSettings.Layout layout = new PlotSettings.Layout();
        public readonly PlotSettings.Axes axes = new PlotSettings.Axes();
        public PlotSettings.Label title, yLabel, xLabel, y2Label;
        public PlotSettings.TickCollection yTicks, xTicks, y2Ticks;

        public Plot()
        {
            title = new PlotSettings.Label { text = "Title", fontSize = 16, bold = true };
            yLabel = new PlotSettings.Label { text = "Vertical Label" };
            xLabel = new PlotSettings.Label { text = "Horzontal Label" };
            y2Label = new PlotSettings.Label { text = "" };

            yTicks = new PlotSettings.TickCollection(PlotSettings.Side.left);
            xTicks = new PlotSettings.TickCollection(PlotSettings.Side.bottom);
            y2Ticks = new PlotSettings.TickCollection(PlotSettings.Side.right);
        }

        #region add or remove plottables

        public void Scatter(double[] xs, double[] ys, Style style = null)
        {
            if (style is null)
                style = new Style(colorIndex: plottables.Count);
            var scatterPlot = new Plottables.Scatter(xs, ys, style);
            plottables.Add(scatterPlot);
        }

        public void Clear()
        {
            plottables.Clear();
        }

        #endregion

        #region axis management

        public void AutoAxis(double marginX = .1, double marginY = .1)
        {
            if (plottables.Count > 0)
            {
                axes.Set(plottables[0].GetDataArea());
                for (int i = 1; i < plottables.Count; i++)
                    axes.Expand(plottables[i].GetDataArea());
            }

            axes.Zoom(1 - marginX, 1 - marginY);
        }

        public void ShareAxis(Plot sharex = null, Plot sharey = null)
        {
            if (sharex != null)
                axes.x = sharex.axes.x;

            if (sharey != null)
                axes.y = sharey.axes.y;
        }

        public void UnShareAxis(bool unshareX = true, bool unshareY = true)
        {
            var axesCopy = new PlotSettings.Axes(axes);
            if (unshareX)
                axes.x = axesCopy.x;
            if (unshareY)
                axes.y = axesCopy.y;
        }

        #endregion

        #region rendering

        public void Render(SKCanvas canvas, SKRect plotRect)
        {
            // update plot-level layout with the latest plot dimensions
            layout.Update(plotRect);

            if (!axes.x.isValid || !axes.y.isValid)
                AutoAxis();

            axes.SetDataRect(layout.dataRect);

            // draw the graphics
            yTicks.FindBestTickDensity(axes.y.low, axes.y.high, layout.dataRect);
            xTicks.FindBestTickDensity(axes.x.low, axes.x.high, layout.dataRect);
            if (yTicks.biggestTickLabelSize.Width > layout.yScaleWidth)
            {
                Debug.WriteLine("increasing Y scale width to prevent overlapping with label Y label");
                layout.yScaleWidth = yTicks.biggestTickLabelSize.Width;
                layout.Update(plotRect);
            }

            var fillPaint = new SKPaint();
            fillPaint.Color = SKColor.Parse("#FFFFFF");
            //fillPaint.Color = Tools.RandomColor(); // useful for assessing when plots are redrawn
            canvas.DrawRect(layout.dataRect, fillPaint);

            yTicks.Render(canvas, axes);
            xTicks.Render(canvas, axes);


            canvas.Save();
            canvas.ClipRect(axes.GetDataRect());

            for (int i = 0; i < plottables.Count; i++)
                plottables[i].Render(canvas, axes);

            canvas.Restore();

            //RenderLayoutDebug(canvas);
            RenderLabels(canvas);
        }

        private void RenderLayoutDebug(SKCanvas canvas)
        {
            SKPaint paintTitle = new SKPaint()
            {
                Color = SKColor.Parse("#55000000"),
                IsAntialias = true
            };

            SKPaint paintLabel = new SKPaint()
            {
                Color = SKColor.Parse("#550000FF"),
                IsAntialias = true
            };

            SKPaint paintScale = new SKPaint()
            {
                Color = SKColor.Parse("#5500FF00"),
                IsAntialias = true
            };

            SKPaint paintData = new SKPaint()
            {
                Color = SKColor.Parse("#55FF0000"),
                IsAntialias = true
            };

            canvas.DrawRect(layout.titleRect, paintTitle);

            canvas.DrawRect(layout.yLabelRect, paintLabel);
            canvas.DrawRect(layout.y2LabelRect, paintLabel);
            canvas.DrawRect(layout.xLabelRect, paintLabel);

            canvas.DrawRect(layout.yScaleRect, paintScale);
            canvas.DrawRect(layout.y2ScaleRect, paintScale);
            canvas.DrawRect(layout.xScaleRect, paintScale);

            canvas.DrawRect(layout.dataRect, paintData);
        }

        private void RenderLabels(SKCanvas canvas)
        {

            SKPaint paintLabel = new SKPaint()
            {
                Color = SKColor.Parse("#000000"),
                TextSize = 16,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true
            };

            int pathExtend = 500;

            SKPath titlePath = new SKPath();
            titlePath.MoveTo(layout.titleRect.Left - pathExtend, layout.titleRect.Top + paintLabel.TextSize);
            titlePath.LineTo(layout.titleRect.Right + pathExtend, layout.titleRect.Top + paintLabel.TextSize);
            canvas.DrawTextOnPath(title.text, titlePath, 0, 0, paintLabel);

            SKPath yLabelPath = new SKPath();
            yLabelPath.MoveTo(layout.yLabelRect.Left + paintLabel.TextSize, layout.yLabelRect.Bottom + pathExtend);
            yLabelPath.LineTo(layout.yLabelRect.Left + paintLabel.TextSize, layout.yLabelRect.Top - pathExtend);
            canvas.DrawTextOnPath(yLabel.text, yLabelPath, 0, 0, paintLabel);

            SKPath y2LabelPath = new SKPath();
            y2LabelPath.MoveTo(layout.y2LabelRect.Right - paintLabel.TextSize, layout.y2LabelRect.Top - pathExtend);
            y2LabelPath.LineTo(layout.y2LabelRect.Right - paintLabel.TextSize, layout.y2LabelRect.Bottom + pathExtend);
            canvas.DrawTextOnPath(y2Label.text, y2LabelPath, 0, 0, paintLabel);

            SKPath xLabelPath = new SKPath();
            xLabelPath.MoveTo(layout.xLabelRect.Left - pathExtend, layout.xLabelRect.Top + paintLabel.TextSize);
            xLabelPath.LineTo(layout.xLabelRect.Right + pathExtend, layout.xLabelRect.Top + paintLabel.TextSize);
            canvas.DrawTextOnPath(xLabel.text, xLabelPath, 0, 0, paintLabel);


            // Outline the data area

            SKPaint layoutFramePaint = new SKPaint()
            {
                Color = SKColor.Parse("#000000"),
                Style = SKPaintStyle.Stroke,
                IsAntialias = false
            };
            canvas.DrawRect(layout.dataRect, layoutFramePaint);
        }

        #endregion
    }
}
