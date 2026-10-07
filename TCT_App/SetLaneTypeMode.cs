using System;
using Gtk;
using Pango;

namespace TCT_App
{
    public partial class SetLaneTypeMode : Gtk.Dialog
    {
        Gdk.Color clBlue = new Gdk.Color(10, 110, 170);
        Gdk.Color clYellow = new Gdk.Color(255, 255, 0);


        public int iOutSelLaneTypeMode;     // 0=No Select, 1=MTC, 2=ETC, 3=DUAL
        //------------------------------------------------------
        public SetLaneTypeMode()
        {
            TCT_Lib.bActiveSetLaneTypeModeDlg = true;

            this.Build();
            this.Decorated = false;     // v2.0.2: no title bar on popups

            TCT_Lib.eCurAppPage = AppPage.SetLaneTypeMode;
            this.Title          = "Set Lane Type";

            InitUI();

            //------------------------------------------------------------
            TCT_Lib.laneController.manageProcessSetLaneTypeMode += LaneController_ManageProcessSetLaneTypeMode;

            this.Activate();
        }

        private void InitUI()
        {
            areaHeader.ModifyBg(StateType.Normal, clBlue);
            areaHeader.QueueDraw();

            btnMode_MTC.Child.ModifyFont(FontDescription.FromString("Arial 18"));
            btnMode_ETC.Child.ModifyFont(FontDescription.FromString("Arial 18"));
            btnMode_Dual.Child.ModifyFont(FontDescription.FromString("Arial 18"));
        }

        protected void OnAreaHeaderExposeEvent(object o, ExposeEventArgs args)
        {
            DrawingArea drawArea    = o as DrawingArea;
            //drawArea.ModifyBg(StateType.Normal, clBlue);
            //---  DrawArea ClassType
            int iWidth              = drawArea.Allocation.Width;
            int iHeight             = drawArea.Allocation.Height;
            Context context         = drawArea.CreatePangoContext();
            Pango.Layout layout     = new Pango.Layout(context);
            layout.Width            = Units.FromPixels(iWidth);
            layout.Alignment        = Pango.Alignment.Center;
            layout.Justify          = true;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 22");
            layout.FontDescription      = fontDesc;

            layout.SetText("โปรดเลือกชนิดของ Lane");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
            //renderer.DrawLayout(layout, 0, 0);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 10, layout, new Gdk.Color(255, 255, 255), clBlue);

            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable = null;
            renderer.Gc = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue], 1);
        }

        protected void OnButtonCancelClicked(object sender, EventArgs e)
        {
            iOutSelLaneTypeMode = 0;
            Action_Cancel();
        }

        private void Action_Cancel()
        {
            this.Respond(ResponseType.Cancel);
            TCT_Lib.laneController.manageProcessSetLaneTypeMode -= LaneController_ManageProcessSetLaneTypeMode;   // v2.0.2: was manageProcessPayConvoy
            TCT_Lib.eCurAppPage                 = AppPage.Login;
            TCT_Lib.bActiveSetLaneTypeModeDlg   = false;
            Dispose();
            Destroy();
        }

        void LaneController_ManageProcessSetLaneTypeMode(int iKey)
        {
            Application.Invoke(delegate
            {
                if (iKey == (int)KeyPressedType.NUM_1)
                {
                    Action_SelMode_MTC();
                }
                else if (iKey == (int)KeyPressedType.NUM_2)
                {
                    Action_SelMode_ETC();
                }
                else if (iKey == (int)KeyPressedType.NUM_3)
                {
                    Action_SelMode_DUAL();
                }
                else if (iKey == (int)KeyPressedType.ESC)
                {
                    Action_Cancel();
                }
            });
        }

        protected void OnBtnModeMTCClicked(object sender, EventArgs e)
        {
            Action_SelMode_MTC();
        }

        protected void OnBtnModeETCClicked(object sender, EventArgs e)
        {
            Action_SelMode_ETC();
        }

        protected void OnBtnModeDualClicked(object sender, EventArgs e)
        {
            Action_SelMode_DUAL();
        }

        protected void Action_SelMode_MTC()
        {
            iOutSelLaneTypeMode = 1;
            Action_Cancel();
        }

        protected void Action_SelMode_ETC()
        {
            iOutSelLaneTypeMode = 2;
            Action_Cancel();
        }

        protected void Action_SelMode_DUAL()
        {
            iOutSelLaneTypeMode = 3;
            Action_Cancel();
        }

    }
}
