#define __FULL_SCREEN__

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Gtk;
using Pango;
using TCT_App.EMV.V0;
using TCT_App.LSD;
using TCT_App.EMV.EMV_V1;

namespace TCT_App.MaintenanceWnd
{
    public partial class MainMaintenance : Gtk.Window
    {
        MainWindow mainWnd;
        bool bTestModeKbd       = false;
        bool bTestModeLSD       = false;
        bool bTestModeTFI       = false;
        bool bTestModeCRS       = false;
        bool bTestModeSCR       = false;
        bool bTestDsrcMain      = false;
        bool bTestDsrcSec       = false;
         
        string szKeyName        = "";
        string szLsdName        = "";
        string szTfiName        = "";
        string szCrsName        = "";
        string szScrName        = "";
        string szDsrcMainName   = "";
        string szDsrcSecName    = "";

        //--- Const Value
        const int ON            = 1;
        const int OFF           = 0;

        Thread threadTestTFI, threadTestCRS, threadTestLSD;
        const string szTFI_StartLane1       = "สวัสดีค่ะ";
        const string szTFI_StartLane2       = "ยินดีให้บริการ";
        //----------------------------------------------------
        Gdk.Color clBlue        = new Gdk.Color(10, 110, 170);
        Gdk.Color clGreen       = new Gdk.Color(0x00, 0xb3, 0x3c);
        Gdk.Color clRed         = new Gdk.Color(0xff, 0x00, 0x00);
        Gdk.Color clBkNormal    = new Gdk.Color(0x30, 0x30, 0x30);
        Gdk.Color clBkActive    = new Gdk.Color(10, 110, 170);
        Gdk.Color clWhite       = new Gdk.Color(0xff, 0xff, 0xff);
        Gdk.Color clBlack       = new Gdk.Color(0, 0, 0);
        Gdk.Color clYellow      = new Gdk.Color(0xff, 0xff, 0x00);
        Gdk.Color clGray        = new Gdk.Color(0x77, 0x77, 0x77);
        //----------------------------------------------------
        int iMainActive = 0, iMenuActive = 0, iSubMenuActive = 0;
        int iCurActive  = 0;    // 0=Main, 1=Menu, 2=SubMenu

        string[] szMainMenu         = { "1 - SIMULATION TEST", "2 - EQUIPMENT TEST", "3 - EXIT MODE" };

        string[] szMenu_Main1       = { "1 - Simulate TCT", "2 - Back" };
        string[] szSub_Main1_Menu1  = { "1 - Run Simulate TCT", "2 - Back" };

        string[] szMenu_Main2       = { "1 - Input / Output Test", "2 - Display Port Test", "3 - Peripheral Test", "4- DSRC Antenna Test", "5 - Back" };
        string[] szSub_Main2_Menu1  = { "1 - Digital Input", "2 - Digital Output", "3 - Treadle Status", "4 - Back" };
        string[] szSub_Main2_Menu2  = { "1 - CRS Test", "2 - TFI Test", "3 - LSD Test", "4 - Back" };
        string[] szSub_Main2_Menu3  = { "1 - Printer Test", "2 - EDC Test", "3 - SCR Test", "4 - Keyboard/Touch Screen",  "5 - Back" };
        string[] szSub_Main2_Menu4  = { "1 - Main Antenna", "2 - Secondary Antenna", "3 - Back" };


        string szTxtMaintenance = "อยู่ระหว่างการซ่อมบำรุง";
        //--------------------------------------------
        public MainMaintenance() :
                base(Gtk.WindowType.Toplevel)
        {
            Build();
            //--- v2.0.2: same window size and header as MainWindow / LoginWnd
            this.WidthRequest           = 1020;
            this.HeightRequest          = 768;
            frameHeader.HeightRequest   = 103;
            frameUserInfo.WidthRequest  = UiTheme.HdrInfo;
            table1.WidthRequest         = UiTheme.HdrTable1;
            table3.WidthRequest         = UiTheme.HdrTable2;
            table5.WidthRequest         = UiTheme.HdrTable3;
            UiTheme.StyleHeader(new[] { lblPlazaName, lblLaneName, lblLaneMode },
                                new[] { lblUserCode, lblUserName, lblUserRole },
                                new[] { lblSupervisor, lblStartShift, lblShiftID },
                                table1, table3, table5);
            UiTheme.StyleStatusBar(statusbar2);
            UiTheme.FlattenFrames(this);
            frameDateTime.WidthRequest  = UiTheme.HdrClock;
            txtTime.Xalign              = 0.5f;
            txtDate.Xalign              = 0.5f;

            Update_Clock();
            InitPanelUI();
            GLib.Timeout.Add(1000, new GLib.TimeoutHandler(Update_Clock));
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : window opened, page {TCT_Lib.eCurAppPage} -> Maintenance, lane mode {TCT_Lib.eCurLaneMode} -> MAINTENANCE, staff {TCT_Lib.szStaffID}");
            TCT_Lib.eCurLaneMode    = LaneMode.MAINTENANCE;
            TCT_Lib.eCurAppPage     = AppPage.Maintenance;

            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Maintenance, (int)TFI_Pattern.Normal, "", "", "", szTxtMaintenance, "");
            //-------------------------------------------------
            TCT_Lib.laneController.maintenanceProcess           += LaneController_MaintenanceProcess;
            TCT_Lib.laneController.exitMaintenanceMode          += LaneController_ExitMaintenanceMode;
            TCT_Lib.laneController.maintenanceSmartCardReader   += LaneController_MaintenanceSmartCardReader;
            TCT_Lib.webSocketLane.laneSocketServerRTO.etcChangeLaneMode += LaneSocketServerRTO_EtcChangeLaneMode;

            this.Activate();
            //--------------------------------------------------------------
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : LSD enabled {TCT_Lib.bEnableLSD}, method {TCT_Lib.iLSD_MethodType}, keepAlive {TCT_Lib.iLSD_KeepAliveSec} s");
            if (TCT_Lib.bEnableLSD)
            {
                TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                LogInfo.WriteLaneEvent("+ Send Command LSD Close (From MaintenanceMode) : " + TCT_Lib.eCurLaneType);
                if (TCT_Lib.iLSD_KeepAliveSec > 0)
                {
                    if (TCT_Lib.iLSD_MethodType == 0)
                        TCT_Lib.lsdController.CmdShow_LaneClose();
                    else
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(LsdMode.LSD_CLOSE);
                }
                else
                {
                    if (TCT_Lib.iLSD_MethodType == 0)
                    {
                        var lsdController = new LaneSocketControl_LSD();
                        lsdController.RunInitialLSD();
                        //lsdController.CmdShow_LaneClose();
                        //lsdController.CloseSocketConnection();
                    }
                    else
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                }
            }


            //-------------------------------------------------------------

        }

        void LaneSocketServerRTO_EtcChangeLaneMode(int iEtcLaneMode, int iEtcLaneState)
        {
            //--- Close Maintenance Mode
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : RTO change lane mode {iEtcLaneMode}, state {iEtcLaneState}{((iEtcLaneMode == (int)LaneMode.MAINTENANCE) && (iEtcLaneState == 0) ? " -> exit maintenance" : "")}");
            if ((iEtcLaneMode == (int)LaneMode.MAINTENANCE) && (iEtcLaneState == 0))
            {
                ProcessExitWnd();
            }
        }

        private bool Update_Clock()
        {
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLog(LogLevel.TRACE, "+ Maintenance : clock tick");
            try
            {
                txtTime.ModifyFont(FontDescription.FromString("Ubuntu Bold 22"));
                txtTime.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
                txtTime.Text = DateTime.Now.ToString("HH:mm:ss");

                txtDate.ModifyFont(FontDescription.FromString("Ubuntu Regular 12"));
                txtDate.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
                txtDate.Text = DateTime.Today.ToString("dd/MM/yyyy", DateTimeExtension.ThaiCultureInfo);   // v2.0.2: BE like the other screens
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.Update_Clock : " + exLog.Message);
                Application.Invoke(delegate
                {
                    txtTime.ModifyFont(FontDescription.FromString("Ubuntu Bold 22"));
                    txtTime.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
                    txtTime.Text = DateTime.Now.ToString("HH:mm:ss");

                    txtDate.ModifyFont(FontDescription.FromString("Ubuntu Regular 12"));
                    txtDate.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
                    txtDate.Text = DateTime.Today.ToString("dd/MM/yyyy", DateTimeExtension.ThaiCultureInfo);   // v2.0.2: BE like the other screens
                });
            }
            finally
            {

            }

            // returning true means that the timeout routine should be invoked
            // again after the timeout period expires.   Returning false would
            // terminate the timeout.

            return true;
        }

        protected void OnDeleteEvent(object o, Gtk.DeleteEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : window delete event -> quit app, page {TCT_Lib.eCurAppPage} -> Login");
            TCT_Lib.bMaintenanceMode = false;
            TCT_Lib.laneController.maintenanceProcess -= LaneController_MaintenanceProcess;
            TCT_Lib.eCurAppPage     = AppPage.Login;
            this.Dispose();
            this.Destroy();
            Application.Quit();
        }

        //--- v2.0.2: ModifyBg() was called inside every expose handler. Each call resets the widget
        //--- style and queues another redraw, and the code also disposed the screen's shared default
        //--- PangoRenderer -> after the first paint this window stopped redrawing (menus never changed,
        //--- clock frozen). Change the background only when the colour actually changes.
        readonly System.Collections.Generic.Dictionary<Widget, Gdk.Color> dicBg = new System.Collections.Generic.Dictionary<Widget, Gdk.Color>();
        void SetBgIfChanged(Widget w, Gdk.Color c)
        {
            if (w == null)
                return;
            Gdk.Color old;
            if (dicBg.TryGetValue(w, out old) && old.Equal(c))
                return;
            dicBg[w] = c;
            w.ModifyBg(StateType.Normal, c);
        }

        protected void InitPanelUI()
        {
            InitPanelHeader();
            InitPanelMaintenance();

            var appVersion = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            Assembly asm = Assembly.GetExecutingAssembly();

            txtVersion.Text = $"<span fgcolor='#ffffff'>Version : {TCT_Lib.szAppVersion}</span>";
            txtVersion.UseMarkup = true;
        }

        protected void InitPanelHeader()
        {
            imgLogo.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Logo.png");
        }

        protected void InitPanelMaintenance()
        {
            SetBgIfChanged(btnMain_1, clBkNormal);
            SetBgIfChanged(btnMain_2, clBkNormal);
            SetBgIfChanged(btnExit, clBkNormal);

            SetBgIfChanged(btnMenu_1, clBkNormal);
            SetBgIfChanged(btnMenu_2, clBkNormal);
            SetBgIfChanged(btnMenu_3, clBkNormal);
            SetBgIfChanged(btnMenu_4, clBkNormal);
            SetBgIfChanged(btnMenu_5, clBkNormal);
            SetBgIfChanged(btnMenu_6, clBkNormal);

            SetBgIfChanged(btnSubMenu_1, clBkNormal);
            SetBgIfChanged(btnSubMenu_2, clBkNormal);
            SetBgIfChanged(btnSubMenu_3, clBkNormal);
            SetBgIfChanged(btnSubMenu_4, clBkNormal);
            SetBgIfChanged(btnSubMenu_5, clBkNormal);
            SetBgIfChanged(btnSubMenu_6, clBkNormal);

            btnMain_1.QueueDraw();
            btnMain_2.QueueDraw();
            btnExit.QueueDraw();

            txtPlazaName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
            txtLaneName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
            txtLaneType.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
            txtStaffID.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
            txtStaffName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
            txtStaffGroup.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
            txtStaffShift.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
            txtSupervisorName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
            //-----------------------------------------------------
            txtPlazaName.Text           = TCT_Lib.szPlazaName;
            txtLaneName.Text            = TCT_Lib.szLaneName;
            txtLaneType.Text            = TCT_Lib.GetCurLaneTypeName();

            txtStaffID.Text             = TCT_Lib.szStaffID;
            txtStaffName.Text           = TCT_Lib.szStaffName;
            txtStaffGroup.Text          = TCT_Lib.szStaffGroupName;
            //txtStaffShift.Text          = TCT_Lib.dtStaffStartShift.ToString("dd-MM-yyyy HH:mm:ss", DateTimeExtension.ThaiCultureInfo);
            txtStaffShift.Text          = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss", DateTimeExtension.ThaiCultureInfo);
            txtSupervisorName.Text      = TCT_Lib.szSupervisorName;

            TCT_Lib.iStaffShiftID       = (int)TCT_Lib.LaneDB.GetStaffShiftID();
            //TCT_Lib.dtStaffStartShift   = DateTime.Now;
        }

        protected void OnBtnExitClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent($"+ Maintenance : Press Exit");
            ProcessExitWnd();
        }

        protected void ProcessExitWnd()
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : exit requested, queued to UI thread");
            Application.Invoke(delegate
            {
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : exit, page {TCT_Lib.eCurAppPage} -> Login, show LoginWnd, lane mode -> CLOSED");
                TCT_Lib.laneController.ClearTextCRS_EndJob();

                TCT_Lib.bMaintenanceMode = false;
                TCT_Lib.eCurAppPage     = AppPage.Login;
                LoginWnd loginWnd       = TCT_Lib.frmLoginWnd;
                Gdk.Color colBk         = new Gdk.Color(0x2E, 0x2E, 0x2E);
                SetBgIfChanged(loginWnd, colBk);

                var screen              = loginWnd.GdkWindow.Screen;
                loginWnd.GdkWindow.GetSize(out var winWidth, out var winHeight);
                loginWnd.Move((screen.Width / 2) - (winWidth / 2), (screen.Height / 2) - (winHeight / 2));

                //loginWnd.SetPosition(WindowPosition.Center);
                loginWnd.bLogined = false;
                loginWnd.Show();
                TCT_Lib.eCurLaneMode    = LaneMode.CLOSED;
                TCT_Lib.szStaffID       = TCT_Lib.szStaffCode = TCT_Lib.szStaffName = "";

                LogInfo.WriteLog("- Exit Maintenance Mode");
                LogInfo.WriteLaneEvent("- Exit Maintenance Mode");
                TCT_Lib.laneController.maintenanceProcess -= LaneController_MaintenanceProcess;
                this.Dispose();
                this.Destroy();
            });
        }

        protected void OnMainExposeEvent(string szAction, bool bActive, DrawingArea btn, ExposeEventArgs args)
        {
            DrawingArea drawArea        = btn;

            int iWidth                  = drawArea.Allocation.Width;
            int iHeight                 = drawArea.Allocation.Height;
            Context context             = drawArea.CreatePangoContext();
            Pango.Layout layout         = new Pango.Layout(context);
            layout.Width                = Units.FromPixels(iWidth);
            layout.Alignment            = Pango.Alignment.Center;
            layout.Justify              = true;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 16");
            layout.FontDescription      = fontDesc;

            layout.SetText(szAction);

            try
            {
                Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
                renderer.Drawable           = drawArea.GdkWindow;
                renderer.Gc                 = drawArea.Style.BlackGC;

                if (iCurActive == 0)
                {
                    SetBgIfChanged(drawArea, bActive ? clBkActive : clBkNormal);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 18, layout, clWhite, bActive ? clBkActive : clBkNormal);
                }
                else
                {
                    SetBgIfChanged(drawArea, bActive ? clGray : clBkNormal);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 18, layout, clWhite, bActive ? clGray : clBkNormal);
                }

                renderer.Drawable   = null;
                renderer.Gc         = null;
                //renderer.Dispose();   // v2.0.2: shared default renderer of the screen - never dispose
                context.Dispose();
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnMainExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
                    renderer.Drawable           = drawArea.GdkWindow;
                    renderer.Gc                 = drawArea.Style.BlackGC;

                    if (iCurActive == 0)
                    {
                        SetBgIfChanged(drawArea, bActive ? clBkActive : clBkNormal);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 18, layout, clWhite, bActive ? clBkActive : clBkNormal);
                    }
                    else
                    {
                        SetBgIfChanged(drawArea, bActive ? clGray : clBkNormal);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 18, layout, clWhite, bActive ? clGray : clBkNormal);
                    }

                    renderer.Drawable   = null;
                    renderer.Gc         = null;
                    //renderer.Dispose();   // v2.0.2: shared default renderer of the screen - never dispose
                    context.Dispose();
                });
            }
            finally
            {

            }

            args.RetVal = true;
        }

        protected void OnBtnMain1ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnMain_1, iMainActive == 1 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnMain1ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnMain_1, iMainActive == 1 ? clBkActive : clBkNormal);
                });
            }
            finally
            {
                DrawingArea drawArea = o as DrawingArea;
                OnMainExposeEvent(szMainMenu[0], iMainActive == 1, drawArea, args);
            }
        }

        protected void OnBtnMain2ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnMain_2, iMainActive == 2 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnMain2ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnMain_2, iMainActive == 2 ? clBkActive : clBkNormal);
                });
            }
            finally
            {
                DrawingArea drawArea = o as DrawingArea;
                OnMainExposeEvent(szMainMenu[1], iMainActive == 2, drawArea, args);
            }
        }

        protected void OnBtnExitExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnExit, iMainActive == 3 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnExitExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnExit, iMainActive == 3 ? clBkActive : clBkNormal);
                });
            }
            finally
            {
                DrawingArea drawArea = o as DrawingArea;
                OnMainExposeEvent(szMainMenu[2], iMainActive == 3, drawArea, args);
            }
        }

        protected void OnBtnMain1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch main menu 1");
            OnBtnMainPressEvent(1);                     
        }

        protected void OnBtnMain2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch main menu 2");
            OnBtnMainPressEvent(2);                      
        }

        protected void OnBtnExitButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent($"+ Maintenance : Press Exit");
            ProcessExitWnd();
        }

        protected void OnBtnMainPressEvent(int iMainMenu)
        {
            LogInfo.WriteLaneEvent($"+ Maintenance : MainMenu={iMainMenu}");
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : main {iMainActive} -> {iMainMenu}, focus {iCurActive} -> 0 (Main), redraw main + menu");
            iCurActive  = 0;
            iMainActive = iMainMenu;
            try
            {
                btnMain_1.QueueDraw();
                btnMain_2.QueueDraw();
                btnExit.QueueDraw();
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.OnBtnMainPressEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    btnMain_1.QueueDraw();
                    btnMain_2.QueueDraw();
                    btnExit.QueueDraw();
                });
            }

            ShowMenu_Main(iMainMenu);
        }

        protected void ShowMenu_Main(int iMenu)
        {
            iMenuActive = 0;
            try
            {
                btnMenu_1.QueueDraw();
                btnMenu_2.QueueDraw();
                btnMenu_3.QueueDraw();
                btnMenu_4.QueueDraw();
                btnMenu_5.QueueDraw();
                btnMenu_6.QueueDraw();
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.ShowMenu_Main : " + exLog.Message);
                Application.Invoke(delegate
                {
                    btnMenu_1.QueueDraw();
                    btnMenu_2.QueueDraw();
                    btnMenu_3.QueueDraw();
                    btnMenu_4.QueueDraw();
                    btnMenu_5.QueueDraw();
                    btnMenu_6.QueueDraw();
                });
            }
        }

        protected void OnMenuExposeEvent(string szAction, bool bActive, DrawingArea btn, ExposeEventArgs args)
        {
            DrawingArea drawArea        = btn;

            int iWidth                  = drawArea.Allocation.Width;
            int iHeight                 = drawArea.Allocation.Height;
            Context context             = drawArea.CreatePangoContext();
            Pango.Layout layout         = new Pango.Layout(context);
            layout.Width                = Units.FromPixels(iWidth);
            layout.Alignment            = Pango.Alignment.Center;
            layout.Justify              = true;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 18");
            layout.FontDescription      = fontDesc;

            layout.SetText(szAction);

            try
            {
                Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
                renderer.Drawable           = drawArea.GdkWindow;
                renderer.Gc                 = drawArea.Style.BlackGC;

                if ((iCurActive == 1) && (btn == btnMenu_6))
                {
                    SetBgIfChanged(drawArea, clYellow);
                    renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                }
                else
                {
                    if (iCurActive == 1)
                    {
                        SetBgIfChanged(drawArea, bActive ? clBkActive : clBkNormal);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clWhite, bActive ? clBkActive : clBkNormal);
                    }
                    else
                    {
                        SetBgIfChanged(drawArea, bActive ? clGray : clBkNormal);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clWhite, bActive ? clGray : clBkNormal);
                    }
                }
                //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
                renderer.Drawable           = null;
                renderer.Gc                 = null;
                //renderer.Dispose();   // v2.0.2: shared default renderer of the screen - never dispose
                context.Dispose();
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnMenuExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
                    renderer.Drawable           = drawArea.GdkWindow;
                    renderer.Gc                 = drawArea.Style.BlackGC;

                    if ((iCurActive == 1) && (btn == btnMenu_6))
                    {
                        SetBgIfChanged(drawArea, clYellow);
                        renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                    }
                    else
                    {
                        if (iCurActive == 1)
                        {
                            SetBgIfChanged(drawArea, bActive ? clBkActive : clBkNormal);
                            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clWhite, bActive ? clBkActive : clBkNormal);
                        }
                        else
                        {
                            SetBgIfChanged(drawArea, bActive ? clGray : clBkNormal);
                            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clWhite, bActive ? clGray : clBkNormal);
                        }
                    }
                    //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
                    renderer.Drawable           = null;
                    renderer.Gc                 = null;
                    //renderer.Dispose();   // v2.0.2: shared default renderer of the screen - never dispose
                    context.Dispose();
                });
            }
            finally
            {

            }

            args.RetVal = true;
        }

        protected void OnBtnMenu1ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnMenu_1, iMenuActive == 1 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnMenu1ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnMenu_1, iMenuActive == 1 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
                szMenu = (szMenu_Main1.Length < 1) ? "" : szMenu_Main1[0];
            else if (iMainActive == 2)
                szMenu = (szMenu_Main2.Length < 1) ? "" : szMenu_Main2[0];
            //else if (iMainActive == 3)
            //    szMenu = (szMenu_Main3.Length < 1) ? "" : szMenu_Main3[0];
            OnMenuExposeEvent(szMenu, iMenuActive == 1, drawArea, args);
        }

        protected void OnBtnMenu2ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnMenu_2, iMenuActive == 2 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnMenu2ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnMenu_2, iMenuActive == 2 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive ==1)
                szMenu = (szMenu_Main1.Length < 2) ? "" : szMenu_Main1[1];
            else if (iMainActive == 2)
                szMenu = (szMenu_Main2.Length < 2) ? "" : szMenu_Main2[1];
            //else if (iMainActive == 3)
            //    szMenu = (szMenu_Main3.Length < 2) ? "" : szMenu_Main3[1];
            OnMenuExposeEvent(szMenu, iMenuActive == 2, drawArea, args);
        }

        protected void OnBtnMenu3ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnMenu_3, iMenuActive == 3 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnMenu3ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnMenu_3, iMenuActive == 3 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
                szMenu = (szMenu_Main1.Length < 3) ? "" : szMenu_Main1[2];
            else if (iMainActive == 2)
                szMenu = (szMenu_Main2.Length < 3) ? "" : szMenu_Main2[2];
            //else if (iMainActive == 3)
            //    szMenu = (szMenu_Main3.Length < 3) ? "" : szMenu_Main3[2];
            OnMenuExposeEvent(szMenu, iMenuActive == 3, drawArea, args);
        }

        protected void OnBtnMenu4ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnMenu_4, iMenuActive == 4 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnMenu4ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnMenu_4, iMenuActive == 4 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
                szMenu = (szMenu_Main1.Length < 4) ? "" : szMenu_Main1[3];
            else if (iMainActive == 2)
                szMenu = (szMenu_Main2.Length < 4) ? "" : szMenu_Main2[3];
            //else if (iMainActive == 3)
            //    szMenu = (szMenu_Main3.Length < 4) ? "" : szMenu_Main3[3];
            OnMenuExposeEvent(szMenu, iMenuActive == 4, drawArea, args);
        }

        protected void OnBtnMenu5ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnMenu_5, iMenuActive == 5 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnMenu5ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnMenu_5, iMenuActive == 5 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
                szMenu = (szMenu_Main1.Length < 5) ? "" : szMenu_Main1[4];
            else if (iMainActive == 2)
                szMenu = (szMenu_Main2.Length < 5) ? "" : szMenu_Main2[4];
            //else if (iMainActive == 3)
            //    szMenu = (szMenu_Main3.Length < 5) ? "" : szMenu_Main3[4];
            OnMenuExposeEvent(szMenu, iMenuActive == 5, drawArea, args);
        }

        protected void OnBtnMenu6ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnMenu_6, iMenuActive == 6 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnMenu6ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnMenu_6, iMenuActive == 6 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
                szMenu = (szMenu_Main1.Length < 6) ? "" : szMenu_Main1[5];
            else if (iMainActive == 2)
                szMenu = (szMenu_Main2.Length < 6) ? "" : szMenu_Main2[5];
            //else if (iMainActive == 3)
            //    szMenu = (szMenu_Main3.Length < 6) ? "" : szMenu_Main3[5];

            if (iCurActive == 1)
                szMenu = "กดลูกศรขวา เพื่อเลือกหัวข้อทดสอบ";

            OnMenuExposeEvent(szMenu, iMenuActive == 6, drawArea, args);
        }

        protected void OnBtnMenuClick(int iMenu)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : menu {iMenuActive} -> {iMenu} (main {iMainActive}), focus {iCurActive} -> 1 (Menu), redraw menu");
            iCurActive  = 1;
            iMenuActive = iMenu;

            try
            {
                btnMenu_1.QueueDraw();
                btnMenu_2.QueueDraw();
                btnMenu_3.QueueDraw();
                btnMenu_4.QueueDraw();
                btnMenu_5.QueueDraw();
                btnMenu_6.QueueDraw();
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.OnBtnMenuClick : " + exLog.Message);
                Application.Invoke(delegate
                {
                    btnMenu_1.QueueDraw();
                    btnMenu_2.QueueDraw();
                    btnMenu_3.QueueDraw();
                    btnMenu_4.QueueDraw();
                    btnMenu_5.QueueDraw();
                    btnMenu_6.QueueDraw();
                });
            }

            if (iMenu > 0)
                LogInfo.WriteLaneEvent($"+ Maintenance : MainMenu={iMainActive} > Menu={iMenuActive}");

            ShowSubMenu(iMenu);
        }

        protected void OnBtnMenu1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch menu 1");
            OnBtnMenuClick(1);
        }

        protected void OnBtnMenu2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch menu 2");
            OnBtnMenuClick(2);
        }

        protected void OnBtnMenu3ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch menu 3");
            OnBtnMenuClick(3);
        }

        protected void OnBtnMenu4ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch menu 4");
            OnBtnMenuClick(4);
        }

        protected void OnBtnMenu5ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch menu 5");
            OnBtnMenuClick(5);
        }

        protected void OnBtnMenu6ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch menu 6");
            OnBtnMenuClick(6);
        }

        protected void ShowSubMenu(int iMenu)
        {
            OnBtnSubMenuClick(0);
/*
            if ((iMainActive == 1) && (iMenu == 1))
            {
                this.Sensitive              = false;

                Gdk.Color colBk             = new Gdk.Color(0x2E, 0x2E, 0x2E);
                TCT_Lib.bMaintenanceMode    = true;
                mainWnd                     = new MainWindow(LoginMethod.MANUAL);
                SetBgIfChanged(mainWnd, colBk);

                var screen                  = mainWnd.GdkWindow.Screen;
                mainWnd.GdkWindow.GetSize(out var winWidth, out var winHeight);
                mainWnd.Move((screen.Width / 2) - (winWidth / 2), (screen.Height / 2) - (winHeight / 2));

                //mainWnd.SetPosition(WindowPosition.Center);
                mainWnd.Show();
                mainWnd.HasFocus            = true;
                mainWnd.Activate();
                this.Sensitive              = true;

#if __FULL_SCREEN__
                mainWnd.Fullscreen();
#endif
            }
            else
            {
                OnBtnSubMenuClick(0);
            }
*/            
        }

        protected void OnSubMenuExposeEvent(string szAction, bool bActive, DrawingArea btn, ExposeEventArgs args)
        {
            DrawingArea drawArea        = btn;

            int iWidth                  = drawArea.Allocation.Width;
            int iHeight                 = drawArea.Allocation.Height;
            Context context             = drawArea.CreatePangoContext();
            Pango.Layout layout         = new Pango.Layout(context);
            layout.Width                = Units.FromPixels(iWidth);
            layout.Alignment            = Pango.Alignment.Center;
            layout.Justify              = true;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 18");
            layout.FontDescription      = fontDesc;

            layout.SetText(szAction);

            try
            {
                Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
                renderer.Drawable           = drawArea.GdkWindow;
                renderer.Gc                 = drawArea.Style.BlackGC;

                if (bTestModeKbd && (btn == btnSubMenu_6))
                {
                    SetBgIfChanged(drawArea, clYellow);
                    renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                }
                else if (bTestModeLSD && (btn == btnSubMenu_6))
                {
                    SetBgIfChanged(drawArea, clYellow);
                    renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                }
                else if (bTestModeTFI && (btn == btnSubMenu_6))
                {
                    SetBgIfChanged(drawArea, clYellow);
                    renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                }
                else if (bTestModeCRS && (btn == btnSubMenu_6))
                {
                    SetBgIfChanged(drawArea, clYellow);
                    renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                }
                else if (bTestModeSCR && (btn == btnSubMenu_6))
                {
                    SetBgIfChanged(drawArea, clYellow);
                    renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                }
                else if (bTestDsrcMain && (btn == btnSubMenu_6))
                {
                    SetBgIfChanged(drawArea, clYellow);
                    renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                }
                else if (bTestDsrcSec && (btn == btnSubMenu_6))
                {
                    SetBgIfChanged(drawArea, clYellow);
                    renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                    drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                }
                else
                {
                    if ((iCurActive == 2) && (btn == btnSubMenu_6))
                    {
                        SetBgIfChanged(drawArea, clYellow);
                        renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                    }
                    else
                    {
                        SetBgIfChanged(drawArea, clBkNormal);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clWhite, clBkNormal);
                    }
                }
                renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
                renderer.Drawable           = null;
                renderer.Gc                 = null;
                //renderer.Dispose();   // v2.0.2: shared default renderer of the screen - never dispose
                context.Dispose();
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnSubMenuExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
                    renderer.Drawable           = drawArea.GdkWindow;
                    renderer.Gc                 = drawArea.Style.BlackGC;

                    if (bTestModeKbd && (btn == btnSubMenu_6))
                    {
                        SetBgIfChanged(drawArea, clYellow);
                        renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                    }
                    else if (bTestModeLSD && (btn == btnSubMenu_6))
                    {
                        SetBgIfChanged(drawArea, clYellow);
                        renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                    }
                    else if (bTestModeTFI && (btn == btnSubMenu_6))
                    {
                        SetBgIfChanged(drawArea, clYellow);
                        renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                    }
                    else if (bTestModeCRS && (btn == btnSubMenu_6))
                    {
                        SetBgIfChanged(drawArea, clYellow);
                        renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                    }
                    else if (bTestModeSCR && (btn == btnSubMenu_6))
                    {
                        SetBgIfChanged(drawArea, clYellow);
                        renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                    }
                    else if (bTestDsrcMain && (btn == btnSubMenu_6))
                    {
                        SetBgIfChanged(drawArea, clYellow);
                        renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                    }
                    else if (bTestDsrcSec && (btn == btnSubMenu_6))
                    {
                        SetBgIfChanged(drawArea, clYellow);
                        renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                    }
                    else
                    {
                        if ((iCurActive == 2) && (btn == btnSubMenu_6))
                        {
                            SetBgIfChanged(drawArea, clYellow);
                            renderer.SetOverrideColor(RenderPart.Foreground, clYellow);
                            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clBlack, clYellow);
                        }
                        else
                        {
                            SetBgIfChanged(drawArea, clBkNormal);
                            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 30, layout, clWhite, clBkNormal);
                        }
                    }

                    renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
                    renderer.Drawable           = null;
                    renderer.Gc                 = null;
                    //renderer.Dispose();   // v2.0.2: shared default renderer of the screen - never dispose
                    context.Dispose();
                });
            }
            finally
            {

            }

            args.RetVal = true;
        }

        protected void OnBtnSubMenu1ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_1, iSubMenuActive == 1 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnSubMenu1ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_1, iSubMenuActive == 1 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main1_Menu1.Length < 1) ? "" : szSub_Main1_Menu1[0];
            }
            else if (iMainActive == 2)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main2_Menu1.Length < 1) ? "" : szSub_Main2_Menu1[0];
                else if (iMenuActive == 2)
                    szMenu = (szSub_Main2_Menu2.Length < 1) ? "" : szSub_Main2_Menu2[0];
                else if (iMenuActive == 3)
                    szMenu = (szSub_Main2_Menu3.Length < 1) ? "" : szSub_Main2_Menu3[0];
                else if (iMenuActive == 4)
                    szMenu = (szSub_Main2_Menu4.Length < 1) ? "" : szSub_Main2_Menu4[0];
            }

            OnSubMenuExposeEvent(szMenu, iSubMenuActive == 1, drawArea, args);
        }

        protected void OnBtnSubMenu2ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_2, iSubMenuActive == 2 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnSubMenu2ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_2, iSubMenuActive == 2 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main1_Menu1.Length < 2) ? "" : szSub_Main1_Menu1[1];                                   
            }
            else if (iMainActive == 2)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main2_Menu1.Length < 2) ? "" : szSub_Main2_Menu1[1];
                else if (iMenuActive == 2)
                    szMenu = (szSub_Main2_Menu2.Length < 2) ? "" : szSub_Main2_Menu2[1];
                else if (iMenuActive == 3)
                    szMenu = (szSub_Main2_Menu3.Length < 2) ? "" : szSub_Main2_Menu3[1];
                else if (iMenuActive == 4)
                    szMenu = (szSub_Main2_Menu4.Length < 2) ? "" : szSub_Main2_Menu4[1];
            }
            OnSubMenuExposeEvent(szMenu, iSubMenuActive == 2, drawArea, args);
        }

        protected void OnBtnSubMenu3ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_3, iSubMenuActive == 3 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnSubMenu3ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_3, iSubMenuActive == 3 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main1_Menu1.Length < 3) ? "" : szSub_Main1_Menu1[2];                                     
            }
            else if (iMainActive == 2)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main2_Menu1.Length < 3) ? "" : szSub_Main2_Menu1[2];
                else if (iMenuActive == 2)
                    szMenu = (szSub_Main2_Menu2.Length < 3) ? "" : szSub_Main2_Menu2[2];
                else if (iMenuActive == 3)
                    szMenu = (szSub_Main2_Menu3.Length < 3) ? "" : szSub_Main2_Menu3[2];
                else if (iMenuActive == 4)
                    szMenu = (szSub_Main2_Menu4.Length < 3) ? "" : szSub_Main2_Menu4[2];
            }
            OnSubMenuExposeEvent(szMenu, iSubMenuActive == 3, drawArea, args);
        }

        protected void OnBtnSubMenu4ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_4, iSubMenuActive == 4 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnSubMenu4ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_4, iSubMenuActive == 4 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main1_Menu1.Length < 4) ? "" : szSub_Main1_Menu1[3];                                   
            }
            else if (iMainActive == 2)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main2_Menu1.Length < 4) ? "" : szSub_Main2_Menu1[3];
                else if (iMenuActive == 2)
                    szMenu = (szSub_Main2_Menu2.Length < 4) ? "" : szSub_Main2_Menu2[3];
                else if (iMenuActive == 3)
                    szMenu = (szSub_Main2_Menu3.Length < 4) ? "" : szSub_Main2_Menu3[3];
                else if (iMenuActive == 4)
                    szMenu = (szSub_Main2_Menu4.Length < 4) ? "" : szSub_Main2_Menu4[3];
            }
            OnSubMenuExposeEvent(szMenu, iSubMenuActive == 4, drawArea, args);
        }

        protected void OnBtnSubMenu5ExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_5, iSubMenuActive == 5 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnSubMenu5ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_5, iSubMenuActive == 5 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main1_Menu1.Length < 5) ? "" : szSub_Main1_Menu1[4];
            }
            else if (iMainActive == 2)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main2_Menu1.Length < 5) ? "" : szSub_Main2_Menu1[4];
                else if (iMenuActive == 2)
                    szMenu = (szSub_Main2_Menu2.Length < 5) ? "" : szSub_Main2_Menu2[4];
                else if (iMenuActive == 3)
                    szMenu = (szSub_Main2_Menu3.Length < 5) ? "" : szSub_Main2_Menu3[4];
                else if (iMenuActive == 4)
                    szMenu = (szSub_Main2_Menu4.Length < 5) ? "" : szSub_Main2_Menu4[4];
            }
            OnSubMenuExposeEvent(szMenu, iSubMenuActive == 5, drawArea, args);
        }

        protected void OnBtnSubMenu6ExposeEvent(object o, ExposeEventArgs args)
        {
            if (bTestModeKbd)
            {
                OnShowKeyPressedExposeEvent(o, args);
                return;
            }
            if (bTestModeLSD)
            {
                OnShowTestLsdExposeEvent(o, args);
                return;
            }
            if (bTestModeTFI)
            {
                OnShowTestTfiExposeEvent(o, args);
                return;
            }
            if (bTestModeCRS)
            {
                OnShowTestCrsExposeEvent(o, args);
                return;
            }
            if (bTestModeSCR)
            {
                OnShowTestScrExposeEvent(o, args);
                return;
            }
            if (bTestDsrcMain)
            {
                OnShowTestDsrcMainExposeEvent(o, args);
                return;
            }
            if (bTestDsrcSec)
            {
                OnShowTestDsrcSecExposeEvent(o, args);
                return;
            }

            try
            {
                SetBgIfChanged(btnSubMenu_6, iSubMenuActive == 6 ? clBkActive : clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnBtnSubMenu6ExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_6, iSubMenuActive == 6 ? clBkActive : clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            string szMenu = "";
            if (iMainActive == 1)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main1_Menu1.Length < 6) ? "" : szSub_Main1_Menu1[5];                                     
            }
            else if (iMainActive == 2)
            {
                if (iMenuActive == 1)
                    szMenu = (szSub_Main2_Menu1.Length < 6) ? "" : szSub_Main2_Menu1[5];
                else if (iMenuActive == 2)
                    szMenu = (szSub_Main2_Menu2.Length < 6) ? "" : szSub_Main2_Menu2[5];
                else if (iMenuActive == 3)
                    szMenu = (szSub_Main2_Menu3.Length < 6) ? "" : szSub_Main2_Menu3[5];
                else if (iMenuActive == 4)
                    szMenu = (szSub_Main2_Menu4.Length < 6) ? "" : szSub_Main2_Menu4[5];
            }
            if (iCurActive == 2)
                szMenu = "กดหมายเลขหัวข้อ เพื่อทดสอบ";
            OnSubMenuExposeEvent(szMenu, iSubMenuActive == 6, drawArea, args);
        }

        protected void OnShowKeyPressedExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_6, clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnShowKeyPressedExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_6, clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            OnSubMenuExposeEvent(szKeyName, false, drawArea, args);
        }

        protected void OnShowTestLsdExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_6, clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnShowTestLsdExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_6, clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            OnSubMenuExposeEvent(szLsdName, false, drawArea, args);
        }

        protected void OnShowTestTfiExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_6, clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnShowTestTfiExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_6, clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            OnSubMenuExposeEvent(szTfiName, false, drawArea, args);
        }

        protected void OnShowTestCrsExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_6, clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnShowTestCrsExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_6, clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            OnSubMenuExposeEvent(szCrsName, false, drawArea, args);
        }

        protected void OnShowTestScrExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_6, clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnShowTestScrExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_6, clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            OnSubMenuExposeEvent(szScrName, false, drawArea, args);
        }

        protected void OnShowTestDsrcMainExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_6, clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnShowTestDsrcMainExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_6, clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            OnSubMenuExposeEvent(szDsrcMainName, false, drawArea, args);
        }

        protected void OnShowTestDsrcSecExposeEvent(object o, ExposeEventArgs args)
        {
            try
            {
                SetBgIfChanged(btnSubMenu_6, clBkNormal);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.TRACE, "- Error Maintenance.OnShowTestDsrcSecExposeEvent : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(btnSubMenu_6, clBkNormal);
                });
            }
            finally
            {

            }

            DrawingArea drawArea = o as DrawingArea;
            OnSubMenuExposeEvent(szDsrcSecName, false, drawArea, args);
        }

        protected void OnBtnSubMenuClick(int iMenu)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : sub menu {iSubMenuActive} -> {iMenu} (main {iMainActive}, menu {iMenuActive}), test TFI {bTestModeTFI} CRS {bTestModeCRS} LSD {bTestModeLSD}, redraw sub menu");
            if (!bTestModeTFI && !bTestModeCRS && !bTestModeLSD)
                ClearActionProcess();

            iSubMenuActive = iMenu;
            try
            {
                btnSubMenu_1.QueueDraw();
                btnSubMenu_2.QueueDraw();
                btnSubMenu_3.QueueDraw();
                btnSubMenu_4.QueueDraw();
                btnSubMenu_5.QueueDraw();
                btnSubMenu_6.QueueDraw();
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.OnBtnSubMenuClick : " + exLog.Message);
                Application.Invoke(delegate
                {
                    btnSubMenu_1.QueueDraw();
                    btnSubMenu_2.QueueDraw();
                    btnSubMenu_3.QueueDraw();
                    btnSubMenu_4.QueueDraw();
                    btnSubMenu_5.QueueDraw();
                    btnSubMenu_6.QueueDraw();
                });
            }

            if (iSubMenuActive > 0)
                LogInfo.WriteLaneEvent($"+ Maintenance : MainMenu={iMainActive} > Menu={iMenuActive} > SubMenu={iSubMenuActive}");
            if (iMainActive == 1)
            {
                if(iMenuActive == 1)
                {
                    if (iSubMenuActive == 1)
                        Process_SimulateTCT();
                }
            }
            if (iMainActive == 2)               // Equipment Test
            {
                if (iMenuActive == 1)               // Input-Output Test
                {
                    if (iSubMenuActive == 1)        // Digital Input Test
                        Process_DigitalInput_Test();
                    else if (iSubMenuActive == 2)   // Digital Output Test
                        Process_DigitalOutput_Test();
                    else if (iSubMenuActive == 3)   // Treadle Status Test
                        Process_TreadleStatus_Test();

                }
                else if (iMenuActive == 2)      // Display Port Test
                {
                    if (iSubMenuActive == 1)        // CRS Test
                        Process_CRS_Test();
                    else if (iSubMenuActive == 2)   // TFI Test
                        Process_TFI_Test();
                    else if (iSubMenuActive == 3)   // LSD Test
                        Process_LSD_Test();
                }
                else if (iMenuActive == 3)      // Peripheral Test
                {
                    if (iSubMenuActive == 1)        // Printer Test
                        Process_Printer_Test();
                    else if (iSubMenuActive == 2)   // EDC Test
                        Process_EDC_Test();
                    else if (iSubMenuActive == 3)   // SCR Test
                        Process_SCR_Test();
                    else if (iSubMenuActive == 4)   // Keyboard and Touch Screen Test
                        Process_KeyPressed_Test();
                }
                else if (iMenuActive == 4)      // DSRC Antenna Test
                {
                    if (iSubMenuActive == 1)        // Main Antenna Test
                        Process_MainAntenna_Test();
                    else if (iSubMenuActive == 2)   // Secondary Antenna Test
                        Process_BackupAntenna_Test();
                }
                else if (iMenuActive == 5)      // Terminal Test
                {
                    if (iSubMenuActive == 1)        // Digital Input Test
                    {
                        Process_Run_Terminal();
                    }
                }
            }
        }

        protected void OnBtnSubMenu1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch sub menu 1");
            OnBtnSubMenuClick(1);
        }

        protected void OnBtnSubMenu2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch sub menu 2");
            OnBtnSubMenuClick(2);
        }

        protected void OnBtnSubMenu3ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch sub menu 3");
            OnBtnSubMenuClick(3);
        }

        protected void OnBtnSubMenu4ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch sub menu 4");
            OnBtnSubMenuClick(4);
        }

        protected void OnBtnSubMenu5ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch sub menu 5");
            OnBtnSubMenuClick(5);
        }

        protected void OnBtnSubMenu6ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : touch sub menu 6");
            OnBtnSubMenuClick(6);
        }

        protected void OnShown(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : shown, dispose simulate MainWindow ({(mainWnd == null ? "null" : "exists")})");
            TCT_Lib.laneController.maintenanceProcess -= LaneController_MaintenanceProcess;
            mainWnd.Dispose();
            mainWnd.Destroy();
            mainWnd = null;

            this.Show();
        }

        [GLib.ConnectBefore]
        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
            {
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : keyboard {args.Event.Key} -> key {(KeyPressedType)TCT_Lib.keyMapping[(int)args.Event.KeyValue]}");
                LaneController_MaintenanceProcess(TCT_Lib.keyMapping[(int)args.Event.KeyValue]);
            }
                //TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue]);
        }

        void LaneController_MaintenanceProcess(int iKey)
        {
            Application.Invoke(delegate
            {
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : key {(KeyPressedType)iKey}, focus {iCurActive} (0=Main 1=Menu 2=Sub), main {iMainActive} menu {iMenuActive} sub {iSubMenuActive}, test Kbd {bTestModeKbd} SCR {bTestModeSCR}");
                if (bTestModeKbd)
                {
                    if (iKey == (int)KeyPressedType.NUM_0)
                    {
                        LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : keyboard test end (key 0)");
                        bTestModeKbd    = false;
                        szKeyName       = "";
                        btnSubMenu_6.QueueDraw();
                    }
                    else
                        Action_ShowTestKeyboard(iKey);
                }
                else if (bTestModeSCR)
                {
                    if (iKey == (int)KeyPressedType.NUM_0)
                    {
                        LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : SCR test end (key 0)");
                        bTestModeSCR    = false;
                        szScrName       = "";
                        btnSubMenu_6.QueueDraw();
                    }
                }
                else
                {
                    //--- Key of Menu
                    if (iCurActive == 0)
                    {
                        //--- Main Menu
                        if (iKey == (int)KeyPressedType.NUM_1)
                            OnBtnMainPressEvent(1);
                        else if (iKey == (int)KeyPressedType.NUM_2)
                            OnBtnMainPressEvent(2);
                        else if (iKey == (int)KeyPressedType.NUM_3)
                            ProcessExitWnd();
                        else if (iKey == (int)KeyPressedType.RIGHT)
                        {
                            if (iMainActive < 3)
                                OnBtnMainPressEvent(iMainActive + 1);
                        }
                        else if (iKey == (int)KeyPressedType.LEFT)
                        {
                            if (iMainActive > 1)
                                OnBtnMainPressEvent(iMainActive - 1);
                        }
                        else if (iKey == (int)KeyPressedType.DOWN)
                        {
                            iCurActive = 1;
                            iMenuActive = 1;
                        }
                        else if (iKey == (int)KeyPressedType.ENTER)
                        {
                            if (iMainActive == 3)
                                ProcessExitWnd();
                        }
                    }
                    else if (iCurActive == 1)
                    {
                        //--- Menu
                        if ((iKey >= (int)KeyPressedType.NUM_1) && (iKey <= (int)KeyPressedType.NUM_9))
                        {
                            if (iMainActive == 1)
                            {
                                if (iKey == (int)KeyPressedType.NUM_1)
                                    OnBtnMenuClick(1);
                                else
                                {
                                    OnBtnMenuClick(0);
                                    iCurActive = 0;
                                }
                            }
                            else if (iMainActive == 2)
                            {
                                if (iKey == (int)KeyPressedType.NUM_1)
                                    OnBtnMenuClick(1);
                                else if (iKey == (int)KeyPressedType.NUM_2)
                                    OnBtnMenuClick(2);
                                else if (iKey == (int)KeyPressedType.NUM_3)
                                    OnBtnMenuClick(3);
                                else if (iKey == (int)KeyPressedType.NUM_4)
                                    OnBtnMenuClick(4);
                                else
                                {
                                    OnBtnMenuClick(0);
                                    iCurActive = 0;
                                }
                            }
                        }
                        else if (iKey == (int)KeyPressedType.DOWN)
                        {
                            OnBtnMenuClick(iMenuActive + 1);
                        }
                        else if (iKey == (int)KeyPressedType.UP)
                        {
                            if (iMenuActive == 1)
                            {
                                OnBtnMenuClick(0);
                                iCurActive = 0;
                            }
                            else
                                OnBtnMenuClick(iMenuActive - 1);
                        }
                        else if (iKey == (int)KeyPressedType.RIGHT)
                        {
                            iCurActive      = 2;
                            iSubMenuActive  = 1;
                        }
                    }
                    else if (iCurActive == 2)
                    {
                        //--- Sub Menu
                        if ((iKey >= (int)KeyPressedType.NUM_1) && (iKey <= (int)KeyPressedType.NUM_9))
                        {
                            if (iMainActive == 1)
                            {
                                if (iMenuActive == 1)
                                {
                                    if (iKey == (int)KeyPressedType.NUM_1)
                                        Process_SimulateTCT();
                                    else
                                        iCurActive = 1;
                                }
                            }
                            else if (iMainActive == 2)
                            {
                                if (iMenuActive == 1)
                                {
                                    //--- Input/Output Test
                                    if (iKey == (int)KeyPressedType.NUM_1)
                                        Process_DigitalInput_Test();
                                    else if (iKey == (int)KeyPressedType.NUM_2)
                                        Process_DigitalOutput_Test();
                                    else if (iKey == (int)KeyPressedType.NUM_3)
                                        Process_TreadleStatus_Test();
                                    else
                                        iCurActive = 1;
                                }
                                else if (iMenuActive == 2)
                                {
                                    //--- Display Port Test
                                    if (iKey == (int)KeyPressedType.NUM_1)
                                    {
                                        if (bTestModeCRS)
                                        {
                                            LogInfo.WriteLaneEvent(LogLevel.WARN, "+ Maintenance : CRS test ignored, already running");
                                            return;
                                        }
                                        Process_CRS_Test();
                                    }
                                    else if (iKey == (int)KeyPressedType.NUM_2)
                                        Process_TFI_Test();
                                    else if (iKey == (int)KeyPressedType.NUM_3)
                                        Process_LSD_Test();
                                    else
                                        iCurActive = 1;
                                }
                                else if (iMenuActive == 3)
                                {
                                    //--- Peripheral Test
                                    if (iKey == (int)KeyPressedType.NUM_1)
                                        Process_Printer_Test();
                                    else if (iKey == (int)KeyPressedType.NUM_2)
                                        Process_EDC_Test();
                                    else if (iKey == (int)KeyPressedType.NUM_3)
                                        Process_SCR_Test();
                                    else if (iKey == (int)KeyPressedType.NUM_4)
                                        Process_KeyPressed_Test();
                                    else
                                        iCurActive = 1;
                                }
                                else if (iMenuActive == 4)
                                {
                                    //--- DSRC Antenna Test
                                    if (iKey == (int)KeyPressedType.NUM_1)
                                        Process_MainAntenna_Test();
                                    else if (iKey == (int)KeyPressedType.NUM_2)
                                        Process_BackupAntenna_Test();
                                    else
                                        iCurActive = 1;
                                }
                                else
                                {
                                    iCurActive = 1;
                                }
                            }
                        }
                        else if (iKey == (int)KeyPressedType.LEFT)
                        {
                            iCurActive = 1;
                            //iMenuActive = 1;
                        }
                    }
                }
            });
        }


        void LaneController_MaintenanceSmartCardReader(string strVal)
        {
            Application.Invoke(delegate
            {
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : smart card read, len {(strVal ?? "").Length}, SCR test {(bTestModeSCR ? "on -> show" : "off -> ignored")}");
                if (LogInfo.IsEnabled(LogLevel.TRACE))
                    LogInfo.WriteLaneEvent(LogLevel.TRACE, "+ Maintenance : smart card value " + (strVal ?? "null"));
                if (bTestModeSCR)
                {
                    szScrName   = strVal;
                    btnSubMenu_6.QueueDraw();
                }
            });
        }

        void LaneController_ExitMaintenanceMode()
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : exit maintenance requested by lane controller");
            Application.Invoke(delegate
            {
                ProcessExitWnd();
            });
        }

        protected void ClearActionProcess()
        {
            if (bTestModeKbd || bTestModeSCR || bTestModeCRS || bTestModeTFI || bTestModeLSD)
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : clear test modes Kbd {bTestModeKbd} SCR {bTestModeSCR} CRS {bTestModeCRS} TFI {bTestModeTFI} LSD {bTestModeLSD}");
            try
            {
                if (bTestModeKbd)
                {
                    bTestModeKbd = false;
                    szKeyName = "";
                    btnSubMenu_6.QueueDraw();
                }
                if (bTestModeSCR)
                {
                    bTestModeSCR = false;
                    szScrName = "";
                    btnSubMenu_6.QueueDraw();
                }
                if (bTestModeCRS)
                {
                    bTestModeCRS = false;
                    szCrsName = "";
                    btnSubMenu_6.QueueDraw();
                }
                if (bTestModeTFI)
                {
                    bTestModeTFI = false;
                    szTfiName = "";
                    btnSubMenu_6.QueueDraw();
                }
                if (bTestModeLSD)
                {
                    bTestModeLSD = false;
                    szLsdName = "";
                    btnSubMenu_6.QueueDraw();
                }
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.ClearActionProcess : " + exLog.Message);
                Application.Invoke(delegate
                {
                    if (bTestModeKbd)
                    {
                        bTestModeKbd = false;
                        szKeyName = "";
                        btnSubMenu_6.QueueDraw();
                    }
                    if (bTestModeSCR)
                    {
                        bTestModeSCR = false;
                        szScrName = "";
                        btnSubMenu_6.QueueDraw();
                    }
                    if (bTestModeCRS)
                    {
                        bTestModeCRS = false;
                        szCrsName = "";
                        btnSubMenu_6.QueueDraw();
                    }
                    if (bTestModeTFI)
                    {
                        bTestModeTFI = false;
                        szTfiName = "";
                        btnSubMenu_6.QueueDraw();
                    }
                    if (bTestModeLSD)
                    {
                        bTestModeLSD = false;
                        szLsdName = "";
                        btnSubMenu_6.QueueDraw();
                    }
                });
            }
        }

        //-----------------------------------------------
        protected void Process_SimulateTCT()
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : simulate TCT -> open MainWindow (maintenance login)");
            this.Sensitive              = false;

            Gdk.Color colBk             = new Gdk.Color(0x2E, 0x2E, 0x2E);
            TCT_Lib.bMaintenanceMode    = true;
            mainWnd                     = new MainWindow(LoginMethod.MANUAL, LoginType.Maintenance);
            TCT_Lib.laneController.ClearAndInitialVar();
            try
            {
                SetBgIfChanged(mainWnd, colBk);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.Process_SimulateTCT : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(mainWnd, colBk);
                });
            }
            finally
            {

            }

            var screen                  = mainWnd.GdkWindow.Screen;
            mainWnd.GdkWindow.GetSize(out var winWidth, out var winHeight);
            mainWnd.Move((screen.Width / 2) - (winWidth / 2), (screen.Height / 2) - (winHeight / 2));

            //mainWnd.SetPosition(WindowPosition.Center);
            mainWnd.Show();
            mainWnd.HasFocus            = true;
            mainWnd.Activate();
            this.Sensitive              = true;
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : simulate MainWindow shown, page {TCT_Lib.eCurAppPage}");
#if __FULL_SCREEN__
                mainWnd.Fullscreen();
#endif
        }

        protected void Process_DigitalInput_Test()
        {
            DigitalInputDlg dlg = new DigitalInputDlg();
            Gdk.Color colBk     = new Gdk.Color(0x2E, 0x2E, 0x2E);
            try
            {
                SetBgIfChanged(dlg, colBk);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.Process_DigitalInput_Test : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(dlg, colBk);
                });
            }
            finally
            {

            }

            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : popup DigitalInputDlg run");
            dlg.Run();
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : popup DigitalInputDlg returned");
        }

        protected void Process_DigitalOutput_Test()
        {
            LaneView.MaintenanceWnd.DigitalOutputDlg dlg = new LaneView.MaintenanceWnd.DigitalOutputDlg();
            Gdk.Color colBk         = new Gdk.Color(0x2E, 0x2E, 0x2E);
            try
            {
                SetBgIfChanged(dlg, colBk);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.Process_DigitalOutput_Test : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(dlg, colBk);
                });
            }
            finally
            {

            }

            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : popup DigitalOutputDlg run");
            dlg.Run();
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : popup DigitalOutputDlg returned");
        }

        protected void Process_TreadleStatus_Test()
        {
            LaneView.MaintenanceWnd.TreadleStatusDlg dlg = new LaneView.MaintenanceWnd.TreadleStatusDlg();
            Gdk.Color colBk = new Gdk.Color(0x2E, 0x2E, 0x2E);
            try
            {
                SetBgIfChanged(dlg, colBk);
            }
            catch (Exception exLog)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.Process_TreadleStatus_Test : " + exLog.Message);
                Application.Invoke(delegate
                {
                    SetBgIfChanged(dlg, colBk);
                });
            }
            finally
            {

            }

            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : popup TreadleStatusDlg run");
            dlg.Run();
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : popup TreadleStatusDlg returned");
        }

        protected void Process_CRS_Test()
        {
            if (bTestModeTFI || bTestModeCRS || bTestModeLSD)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ Maintenance : CRS test ignored, test running (TFI {bTestModeTFI} CRS {bTestModeCRS} LSD {bTestModeLSD})");
                return;
            }
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : CRS test start");

            ProcessTestCRS();
           
/*                 
            threadTestCRS               = new Thread(new ThreadStart(TheadProcessTestCRS));
            threadTestCRS.Priority      = ThreadPriority.Normal;
            threadTestCRS.IsBackground  = true;
            threadTestCRS.Start();
*/                       
        }

        protected async void ProcessTestCRS()
        {
            TCT_Lib.bAllowSendRTO   = false;
            bTestModeCRS            = true;
            LogInfo.WriteLaneEvent("+ Start Process_CRS_Test");
            szCrsName               = "Testing CRS";
            //btnSubMenu_6.QueueDraw();
            DrawText(btnSubMenu_6);

            try
            {
                //Application.Invoke(async delegate
                //{
                    //----------------------------------------------
/*
                    if (TCT_Lib.szMTS_Monitor_CRS_IPAddress != "")
                    {
                        if (!TCT_Lib.TestConnection(TCT_Lib.szMTS_Monitor_CRS_IPAddress, TCT_Lib.iMTS_Monitor_CRS_Port))
                        {
                            bTestModeCRS            = false;
                            TCT_Lib.bAllowSendRTO   = true;
                            szCrsName               = "CRS - Fail Connection";
                            //btnSubMenu_6.QueueDraw();
                            DrawText(btnSubMenu_6);
                            LogInfo.WriteLaneEvent("- Error Test Process_CRS_Test : " + "Fail Connection CRS");
                            await Task.Delay(1000);
                            //threadTestCRS.Abort();
                            return;
                        }
                    }
*/                    
                //});
                LogInfo.WriteLaneEvent($"+ Successs Connect CRS : IP={TCT_Lib.szMTS_Monitor_CRS_IPAddress}, Port={TCT_Lib.iMTS_Monitor_CRS_Port}");

                //----------------------------------------------

                int iSleep = 2 * 1000;

                LogInfo.WriteLaneEvent("+ CRS -> 1 C");
                TCT_Lib.webSocketLane.OutIo_WriteCRS("1", "C");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ CRS -> 2 V");
                TCT_Lib.webSocketLane.OutIo_WriteCRS("2", "V");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ CRS -> 1 T");
                TCT_Lib.webSocketLane.OutIo_WriteCRS("1", "T");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ CRS -> 2 M");
                TCT_Lib.webSocketLane.OutIo_WriteCRS("2", "M");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ CRS -> 1 Q");
                TCT_Lib.webSocketLane.OutIo_WriteCRS("1", "Q");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ CRS -> H");
                TCT_Lib.webSocketLane.OutIo_WriteCRS("0", "H");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                TCT_Lib.webSocketLane.OutIo_ClearCRS();

                //Application.Invoke(async delegate
                //{
                    bTestModeCRS            = false;
                    TCT_Lib.bAllowSendRTO   = true;
                    szCrsName               = "";
                    //btnSubMenu_6.QueueDraw();
                    DrawText(btnSubMenu_6);
                //});
            }
            catch (Exception err)
            {
                bTestModeCRS            = false;
                TCT_Lib.bAllowSendRTO   = true;
                LogInfo.WriteLaneEvent("- Error Test CRS : " + err.Message);
            }
        }

        protected void TheadProcessTestCRS()
        {
            TCT_Lib.bAllowSendRTO   = false;
            bTestModeCRS            = true;
            LogInfo.WriteLaneEvent("+ Start Process_CRS_Test");

            try
            {
                Application.Invoke(async delegate
                {
                    //----------------------------------------------
/*
                    if (TCT_Lib.szMTS_Monitor_CRS_IPAddress != "")
                    {
                        if (!TCT_Lib.TestConnection(TCT_Lib.szMTS_Monitor_CRS_IPAddress, TCT_Lib.iMTS_Monitor_CRS_Port))
                        {
                            bTestModeCRS            = false;
                            TCT_Lib.bAllowSendRTO   = true;
                            szCrsName               = "CRS - Fail Connection";
                            btnSubMenu_6.QueueDraw();
                            LogInfo.WriteLaneEvent("- Error Process_CRS_Test : " + "Fail Connection");
                            await Task.Delay(1000);
                            //threadTestCRS.Abort();
                            return;
                        }
                    }
*/
                //----------------------------------------------

                    int iSleep = 6 * 1000;

                    LogInfo.WriteLaneEvent("+ CRS -> 1 C");
                    szCrsName       = "CRS - 1 C";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteCRS("1", "C");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    LogInfo.WriteLaneEvent("+ CRS -> 2 V");
                    szCrsName       = "CRS -2 V";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteCRS("2", "V");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(1000);

                    LogInfo.WriteLaneEvent("+ CRS -> 1 T");
                    szCrsName       = "CRS - 1 T";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteCRS("1", "T");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    LogInfo.WriteLaneEvent("+ CRS -> 2 M");
                    szCrsName       = "CRS - 2 M";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteCRS("2", "M");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    LogInfo.WriteLaneEvent("+ CRS -> 1 Q");
                    szCrsName       = "CRS - 1 Q";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteCRS("1", "Q");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

/*
                    LogInfo.WriteLaneEvent("+ CRS -> 2 E");
                    szCrsName       = "CRS - 2 E";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteCRS("2", "E");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);
*/                                       

                    LogInfo.WriteLaneEvent("+ CRS -> H");
                    szCrsName       = "CRS - 0 H";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteCRS("0", "H");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(1000);

                    //LogInfo.WriteLaneEvent("+ CRS -> L");
                    //szCrsName       = "CRS - L";
                    //btnSubMenu_6.QueueDraw();
                    //TCT_Lib.webSocketLane.OutIo_WriteCRS("0", "L");
                    //await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    szCrsName       = "CRS - Clear";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_ClearCRS();
                    await Task.Delay(2000);
                    //Thread.Sleep(iSleep);

                    bTestModeCRS            = false;
                    TCT_Lib.bAllowSendRTO   = true;
                    szCrsName               = "";
                    btnSubMenu_6.QueueDraw();

                    await Task.Delay(iSleep);
                    LogInfo.WriteLaneEvent("+ CRS -> End");
                    //threadTestCRS.Abort();
                });
            }
            catch (Exception err)
            {
                bTestModeCRS            = false;
                TCT_Lib.bAllowSendRTO   = true;
                LogInfo.WriteLaneEvent("- Error Test CRS : " + err.Message);
                //threadTestCRS.Abort();
            }
        }

        protected void Process_TFI_Test()
        {
            if (bTestModeTFI || bTestModeCRS || bTestModeLSD)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ Maintenance : TFI test ignored, test running (TFI {bTestModeTFI} CRS {bTestModeCRS} LSD {bTestModeLSD})");
                return;
            }
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : TFI test start");

            ProcessTestTFIAsync();

/*
            threadTestTFI               = new Thread(new ThreadStart(ThreadProcessTestTFI));
            threadTestTFI.Priority      = ThreadPriority.Normal;
            threadTestTFI.IsBackground  = true;
            threadTestTFI.Start();
*/                       
        }

        protected async Task ProcessTestTFIAsync()
        {
            const string szTFI_StartLane1       = "สวัสดีค่ะ";
            const string szTFI_StartLane2       = "ยินดีให้บริการ";
            const string szTFI_CloseLane        = "ปิดช่องทาง";

            const string szVehicle_Type1        = "รถประเภท 1";
            const string szVehicle_Type2        = "รถประเภท 2";
            const string szVehicle_Emergency    = "รถฉุกเฉิน";
            const string szVehicle_HPMC         = "รถขบวน";
            const string szVehicle_LeftExit     = "รถออกซ้าย";

            string szAmount_Class1              = TCT_Lib.fFareClass1.ToString("0") + " บาท";
            string szAmount_Class2              = TCT_Lib.fFareClass2.ToString("0") + " บาท";
            string szAmount_0                   = "0 บาท";
            //--------------------------------------------------------------
            TCT_Lib.bAllowSendRTO   = false;
            bTestModeTFI            = true;
            LogInfo.WriteLaneEvent("+ Start Process_TFI_Test");
            szTfiName               = "Testing TFI";
            //btnSubMenu_6.QueueDraw();
            DrawText(btnSubMenu_6);

            try
            {
                //Application.Invoke(async delegate
                //{
                    //----------------------------------------------
/*
                    if (TCT_Lib.szMTS_Monitor_TFI_IPAddress != "")
                    {
                        if (!TCT_Lib.TestConnection(TCT_Lib.szMTS_Monitor_TFI_IPAddress, TCT_Lib.iMTS_Monitor_TFI_Port))
                        {
                            szTfiName               = "TFI - Fail Connection";
                            //btnSubMenu_6.QueueDraw();
                            DrawText(btnSubMenu_6);
                            LogInfo.WriteLaneEvent("- Error Process_TFI_Test : " + "Fail Connection");
                            await Task.Delay(1000);
                            bTestModeTFI            = false;
                            TCT_Lib.bAllowSendRTO   = true;
                            //threadTestTFI.Abort();
                            return;
                        }
                    }
*/                    
                //});

                //----------------------------------------------

                int iSleep = 2 * 1000;

                LogInfo.WriteLaneEvent("+ TFI -> Start TFI");
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.StartSceen, (int)TFI_Pattern.Normal, "", "", "", "", "");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ TFI -> Welcome");
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ TFI -> Class 1");
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "1", TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ TFI -> HPMC");
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.HPMC, (int)TFI_Pattern.HPMC, "0", "0", "", szVehicle_HPMC, szAmount_0);
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ TFI -> Thank you");
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.ThankYou, (int)TFI_Pattern.Normal, "", "", "", "");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ TFI -> Maintenance");
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Maintenance, (int)TFI_Pattern.Normal, "", "", "", "Maintenance", "");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                LogInfo.WriteLaneEvent("+ TFI -> Close Lane");
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                await Task.Delay(iSleep);
                //Thread.Sleep(iSleep);

                //Application.Invoke(async delegate
                //{
                    bTestModeTFI            = false;
                    TCT_Lib.bAllowSendRTO   = true;
                    szTfiName               = "";
                    //btnSubMenu_6.QueueDraw();
                    DrawText(btnSubMenu_6);
                //await Task.Delay(1000);
                //});
            }
            catch (Exception err)
            {
                bTestModeTFI            = false;
                TCT_Lib.bAllowSendRTO   = true;
                LogInfo.WriteLaneEvent("- Error Test TFI : " + err.Message);
            }
        }

        protected void ThreadProcessTestTFI()
        {
            const string szTFI_StartLane1       = "สวัสดีค่ะ";
            const string szTFI_StartLane2       = "ยินดีให้บริการ";
            const string szTFI_CloseLane        = "ปิดช่องทาง";

            const string szVehicle_Type1        = "รถประเภท 1";
            const string szVehicle_Type2        = "รถประเภท 2";
            const string szVehicle_Emergency    = "รถฉุกเฉิน";
            const string szVehicle_HPMC         = "รถขบวน";
            const string szVehicle_LeftExit     = "รถออกซ้าย";

            string szAmount_Class1              = TCT_Lib.fFareClass1.ToString("0") + " บาท";
            string szAmount_Class2              = TCT_Lib.fFareClass2.ToString("0") + " บาท";
            string szAmount_0                   = "0 บาท";
            //--------------------------------------------------------------
            TCT_Lib.bAllowSendRTO   = false;
            bTestModeTFI            = true;
            LogInfo.WriteLaneEvent("+ Start Process_TFI_Test");

            try
            {
                Application.Invoke(async delegate
                {
                    //----------------------------------------------
/*
                    if (TCT_Lib.szMTS_Monitor_TFI_IPAddress != "")
                    {
                        if (!TCT_Lib.TestConnection(TCT_Lib.szMTS_Monitor_TFI_IPAddress, TCT_Lib.iMTS_Monitor_TFI_Port))
                        {
                            szTfiName               = "TFI - Fail Connection";
                            btnSubMenu_6.QueueDraw();
                            LogInfo.WriteLaneEvent("- Error Process_TFI_Test : " + "Fail Connection");
                            await Task.Delay(1000);
                            bTestModeTFI            = false;
                            TCT_Lib.bAllowSendRTO   = true;
                            //threadTestTFI.Abort();
                            return;
                        }
                    }
*/                    
                    //----------------------------------------------

                    int iSleep = 5 * 1000;

                    LogInfo.WriteLaneEvent("+ TFI -> Start TFI");
                    szTfiName       = "TFI - Start";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.StartSceen, (int)TFI_Pattern.Normal, "", "", "", "", "");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    LogInfo.WriteLaneEvent("+ TFI -> Welcome");
                    szTfiName       = "TFI - Welcome";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    LogInfo.WriteLaneEvent("+ TFI -> Class 1");
                    szTfiName       = "TFI - Class 1";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "1", TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

/*
                    LogInfo.WriteLaneEvent("+ TFI -> Class 2");
                    szTfiName = "TFI - Class 2";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "2", TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);
*/                                       

                    LogInfo.WriteLaneEvent("+ TFI -> HPMC");
                    szTfiName       = "TFI - HPMC";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.HPMC, (int)TFI_Pattern.HPMC, "0", "0", "", szVehicle_HPMC, szAmount_0);
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    LogInfo.WriteLaneEvent("+ TFI -> Thank you");
                    szTfiName       = "TFI - Thank you";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.ThankYou, (int)TFI_Pattern.Normal, "", "", "", "");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    LogInfo.WriteLaneEvent("+ TFI -> Maintenance");
                    szTfiName       = "TFI - Maintenance";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Maintenance, (int)TFI_Pattern.Normal, "", "", "", "Maintenance", "");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    LogInfo.WriteLaneEvent("+ TFI -> Close Lane");
                    szTfiName       = "TFI - Close Lane";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                    await Task.Delay(iSleep);
                    //Thread.Sleep(iSleep);

                    bTestModeTFI            = false;
                    TCT_Lib.bAllowSendRTO   = true;
                    szTfiName               = "";
                    btnSubMenu_6.QueueDraw();
                    //await Task.Delay(1000);
                    //threadTestTFI.Abort();

/*
                    LogInfo.WriteLaneEvent("+ TFI -> Clear");
                    szTfiName = "TFI - Clear";
                    btnSubMenu_6.QueueDraw();
                    TCT_Lib.webSocketLane.OutIo_ClearTFI();
                    //await Task.Delay(2000);
                    //Thread.Sleep(2000);

                    //LogInfo.WriteLaneEvent("+ TFI -> End");
                    bTestModeTFI            = false;
                    TCT_Lib.bAllowSendRTO   = true;
                    szTfiName               = "";
                    btnSubMenu_6.QueueDraw();
                    //await Task.Delay(1000);
                    threadTestTFI.Abort();
*/                                       
                });
            }
            catch (Exception err)
            {
                bTestModeTFI            = false;
                TCT_Lib.bAllowSendRTO   = true;
                LogInfo.WriteLaneEvent("- Error Test TFI : " + err.Message);
                //threadTestTFI.Abort();
            }
        }

        protected void Process_LSD_Test()
        {
        
            if (bTestModeTFI || bTestModeCRS || bTestModeLSD)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ Maintenance : LSD test ignored, test running (TFI {bTestModeTFI} CRS {bTestModeCRS} LSD {bTestModeLSD})");
                return;
            }
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : LSD test start");

            ProcessTestLSD();

/*
            threadTestLSD               = new Thread(new ThreadStart(ThreadProcessTestLSD));
            threadTestLSD.Priority      = ThreadPriority.Normal;
            threadTestLSD.IsBackground  = true;
            threadTestLSD.Start();
*/                       
        }

        protected void DrawText(DrawingArea obj)
        {
            Application.Invoke(async delegate
            {
                obj.QueueDraw();
            });
        }

        protected async void ProcessTestLSD()
        {
            TCT_Lib.bAllowSendRTO   = false;
            bTestModeLSD            = true;
            LogInfo.WriteLaneEvent("+ Start Process_LSD_Test");
            szLsdName               = "Testing LSD";
            //btnSubMenu_6.QueueDraw();
            DrawText(btnSubMenu_6);

            try
            {
                if (!TCT_Lib.bEnableLSD)
                {
                    //Application.Invoke(async delegate
                    //{
                        LogInfo.WriteLaneEvent("- No Enable LSD.");
                        szLsdName               = "No Enable LSD";
                        //btnSubMenu_6.QueueDraw();
                        DrawText(btnSubMenu_6);
                        Thread.Sleep(2000);
                        bTestModeLSD            = false;
                        TCT_Lib.bAllowSendRTO   = true;
                        szLsdName               = "";
                        //btnSubMenu_6.QueueDraw();
                        DrawText(btnSubMenu_6);
                    //});
                }
                else
                {
/*
                    if (!TCT_Lib.TestConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort))
                    {
                        //Application.Invoke(async delegate
                        //{
                            szLsdName               = "LSD - Fail Connection";
                            //btnSubMenu_6.QueueDraw();
                            DrawText(btnSubMenu_6);
                            LogInfo.WriteLaneEvent("- Error Process_LSD_Test : " + "Fail Connection");
                            await Task.Delay(1000);
                            //Thread.Sleep(1000);
                            bTestModeLSD            = false;
                            TCT_Lib.bAllowSendRTO   = true;
                            //threadTestLSD.Abort();
                        //});
                        return;
                    }
*/                    
                    //----------------------------------------------
                    int iSleep = 6 * 1000;
                    if (TCT_Lib.iLSD_MethodType == 0)
                    {
                        if (TCT_Lib.iLSD_KeepAliveSec > 0)
                        {
                            LogInfo.WriteLaneEvent("+ Test LSD KeepAliveSec > 0");
                            var lsdController = new LaneSocketControl_LSD();
                            await lsdController.RunInitialLSD(false);

                            LogInfo.WriteLaneEvent("+ LSD -> Open MTC");
                            TCT_Lib.eCurLsdMode = LsdMode.LSD_MTC;
                            await lsdController.CmdShow_OpenMTC();
                            await Task.Delay(iSleep);
                            //Thread.Sleep(iSleep);

                            LogInfo.WriteLaneEvent("+ LSD -> Open ETC");
                            TCT_Lib.eCurLsdMode = LsdMode.LSD_ETC;
                            await lsdController.CmdShow_OpenETC();
                            await Task.Delay(iSleep);
                            //Thread.Sleep(iSleep);

                            LogInfo.WriteLaneEvent("+ LSD -> Open DUAL");
                            TCT_Lib.eCurLsdMode = LsdMode.LSD_DUAL;
                            await lsdController.CmdShow_OpenDual();
                            await Task.Delay(iSleep);
                            //Thread.Sleep(iSleep);

                            LogInfo.WriteLaneEvent("+ LSD -> Lane Close");
                            TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                            await lsdController.CmdShow_LaneClose();
                            await Task.Delay(iSleep);
                            //Thread.Sleep(iSleep);

                            lsdController.CloseSocketConnection();
                        }
                        else
                        {
                            LogInfo.WriteLaneEvent("+ Test LSD KeepAliveSec = 0");

                            LogInfo.WriteLaneEvent("+ LSD -> Open MTC");
                            szLsdName = "LSD - Open MTC";
                            //btnSubMenu_6.QueueDraw();
                            DrawText(btnSubMenu_6);
                            var lsdController   = new LaneSocketControl_LSD();
                            await lsdController.RunInitialLSD(false);
                            await Task.Delay(1000);
                            //Thread.Sleep(1000);
                            TCT_Lib.eCurLsdMode = LsdMode.LSD_MTC;
                            await lsdController.CmdShow_OpenMTC();
                            //lsdController.CloseSocketConnection();
                            await Task.Delay(iSleep);
                            //Thread.Sleep(iSleep);

                            LogInfo.WriteLaneEvent("+ LSD -> Open ETC");
                            szLsdName = "LSD - Open ETC";
                            //btnSubMenu_6.QueueDraw();
                            DrawText(btnSubMenu_6);
                            //lsdController       = new LaneSocketControl_LSD();
                            TCT_Lib.eCurLsdMode = LsdMode.LSD_ETC;
                            await lsdController.CmdShow_OpenETC();
                            //lsdController.CloseSocketConnection();
                            await Task.Delay(iSleep);
                            //Thread.Sleep(iSleep);

                            LogInfo.WriteLaneEvent("+ LSD -> Open DUAL");
                            szLsdName = "LSD - Open DUAL";
                            //btnSubMenu_6.QueueDraw();
                            DrawText(btnSubMenu_6);
                            //lsdController       = new LaneSocketControl_LSD();
                            TCT_Lib.eCurLsdMode = LsdMode.LSD_DUAL;
                            await lsdController.CmdShow_OpenDual();
                            //lsdController.CloseSocketConnection();
                            await Task.Delay(iSleep);
                            //Thread.Sleep(iSleep);

                            LogInfo.WriteLaneEvent("+ LSD -> Lane Close");
                            szLsdName = "LSD - Lane Close";
                            //btnSubMenu_6.QueueDraw();
                            DrawText(btnSubMenu_6);
                            //lsdController       = new LaneSocketControl_LSD();
                            TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                            await lsdController.CmdShow_LaneClose();
                            await Task.Delay(2*1000);
                            //Thread.Sleep(2 * 1000);
                            lsdController.CloseSocketConnection();
                        }
                    }
                    else
                    {
                        LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : LSD test via RTO (method 1)");
                        szLsdName = "LSD - Testing";
                        //btnSubMenu_6.QueueDraw();
                        DrawText(btnSubMenu_6);
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendTestLsdMaintenanceToRTO();
                    }

                    LogInfo.WriteLaneEvent("+ LSD -> End");
                    //Application.Invoke(async delegate
                    //{
                        bTestModeLSD    = false;
                        TCT_Lib.bAllowSendRTO = true;
                        szLsdName       = "";
                        //btnSubMenu_6.QueueDraw();
                        DrawText(btnSubMenu_6);
                        await Task.Delay(1000);
                    //});
                }
            }
            catch (Exception err)
            {
                bTestModeLSD            = false;
                TCT_Lib.bAllowSendRTO   = true;
                LogInfo.WriteLaneEvent("- Error Process_LSD_Test : " + err.Message);
            }
        }

        protected async Task ThreadProcessTestLSDAsync()
        {
            TCT_Lib.bAllowSendRTO   = false;
            bTestModeLSD            = true;
            LogInfo.WriteLaneEvent("+ Start Process_LSD_Test");

            try
            {
                //Application.Invoke(async delegate
                //{
                    if (!TCT_Lib.bEnableLSD)
                    {
                        LogInfo.WriteLaneEvent("- No Enable LSD.");
                        szLsdName               = "No Enable LSD";
                        //btnSubMenu_6.QueueDraw();
                        DrawText(btnSubMenu_6);
                        Thread.Sleep(2000);
                        bTestModeLSD            = false;
                        TCT_Lib.bAllowSendRTO   = true;
                        szLsdName               = "";
                        //btnSubMenu_6.QueueDraw();
                        DrawText(btnSubMenu_6);
                    }
                    else
                    {
/*
                        if (!TCT_Lib.TestConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort))
                        {
                            szLsdName               = "LSD - Fail Connection";
                            //btnSubMenu_6.QueueDraw();
                            DrawText(btnSubMenu_6);
                            LogInfo.WriteLaneEvent("- Error Process_LSD_Test : " + "Fail Connection");
                            await Task.Delay(1000);
                            bTestModeLSD            = false;
                            TCT_Lib.bAllowSendRTO   = true;
                            threadTestLSD.Abort();
                            return;
                        }
*/                        
                        //----------------------------------------------
                        int iSleep = 8 * 1000;
                        if (TCT_Lib.iLSD_MethodType == 0)
                        {
                            await TCT_Lib.lsdController.RunInitialLSD();

                            if (TCT_Lib.iLSD_KeepAliveSec > 0)
                            {
                                LogInfo.WriteLaneEvent("+ LSD -> Open MTC");
                                szLsdName = "LSD - Open MTC";
                                //btnSubMenu_6.QueueDraw();
                                DrawText(btnSubMenu_6);
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_MTC;
                                await TCT_Lib.lsdController.CmdShow_OpenMTC();
                                await Task.Delay(iSleep);
                                //Thread.Sleep(iSleep);

                                LogInfo.WriteLaneEvent("+ LSD -> Open ETC");
                                szLsdName = "LSD - Open ETC";
                                //btnSubMenu_6.QueueDraw();
                                DrawText(btnSubMenu_6);
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_ETC;
                                await TCT_Lib.lsdController.CmdShow_OpenETC();
                                await Task.Delay(iSleep);
                                //Thread.Sleep(iSleep);

                                LogInfo.WriteLaneEvent("+ LSD -> Open DUAL");
                                szLsdName           = "LSD - Open DUAL";
                                //btnSubMenu_6.QueueDraw();
                                DrawText(btnSubMenu_6);
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_DUAL;
                                await TCT_Lib.lsdController.CmdShow_OpenDual();
                                await Task.Delay(iSleep);
                                //Thread.Sleep(iSleep);

                                LogInfo.WriteLaneEvent("+ LSD -> Lane Close");
                                szLsdName           = "LSD - Lane Close";
                                //btnSubMenu_6.QueueDraw();
                                DrawText(btnSubMenu_6);
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                                await TCT_Lib.lsdController.CmdShow_LaneClose();
                                await Task.Delay(iSleep);
                                //Thread.Sleep(iSleep);
                            }
                            else
                            {
                                LogInfo.WriteLaneEvent("+ LSD -> Open MTC");
                                szLsdName           = "LSD - Open MTC";
                                //btnSubMenu_6.QueueDraw();
                                DrawText(btnSubMenu_6);
                                var lsdController = new LaneSocketControl_LSD();
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_MTC;
                                await lsdController.CmdShow_OpenMTC();
                                //lsdController.CloseSocketConnection();
                                await Task.Delay(iSleep);
                                //Thread.Sleep(iSleep);

                                LogInfo.WriteLaneEvent("+ LSD -> Open ETC");
                                szLsdName           = "LSD - Open ETC";
                                //btnSubMenu_6.QueueDraw();
                                DrawText(btnSubMenu_6);
                                //lsdController       = new LaneSocketControl_LSD();
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_ETC;
                                await lsdController.CmdShow_OpenETC();
                                //lsdController.CloseSocketConnection();
                                await Task.Delay(iSleep);
                                //Thread.Sleep(iSleep);

                                LogInfo.WriteLaneEvent("+ LSD -> Open DUAL");
                                szLsdName           = "LSD - Open DUAL";
                                //btnSubMenu_6.QueueDraw();
                                DrawText(btnSubMenu_6);
                                //lsdController       = new LaneSocketControl_LSD();
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_DUAL;
                                await lsdController.CmdShow_OpenDual();
                                lsdController.CloseSocketConnection();
                                await Task.Delay(iSleep);
                                //Thread.Sleep(iSleep);

                                LogInfo.WriteLaneEvent("+ LSD -> Lane Close");
                                szLsdName           = "LSD - Lane Close";
                                //btnSubMenu_6.QueueDraw();
                                DrawText(btnSubMenu_6);
                                //lsdController       = new LaneSocketControl_LSD();
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                                await lsdController.CmdShow_LaneClose();
                                lsdController.CloseSocketConnection();
                                await Task.Delay(2000);
                            }
                        }
                        else
                        {
                            szLsdName = "LSD - Testing";
                            //btnSubMenu_6.QueueDraw();
                            DrawText(btnSubMenu_6);
                            TCT_Lib.webSocketLane.laneSocketServerRTO.SendTestLsdMaintenanceToRTO();
                        }

                        LogInfo.WriteLaneEvent("+ LSD -> End");
                        bTestModeLSD            = false;
                        TCT_Lib.bAllowSendRTO   = true;
                        szLsdName               = "";
                        //btnSubMenu_6.QueueDraw();
                        DrawText(btnSubMenu_6);
                        await Task.Delay(1000);
                        threadTestLSD.Abort();
                    }
                //});
            }
            catch (ThreadAbortException)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, "- Maintenance : LSD test thread aborted");
                bTestModeLSD            = false;
                TCT_Lib.bAllowSendRTO   = true;
            }
            catch (Exception err)
            {
                bTestModeLSD            = false;
                TCT_Lib.bAllowSendRTO   = true;
                LogInfo.WriteLaneEvent("- Error Process_LSD_Test : " + err.Message);
                //threadTestLSD.Abort();
            }
        }

        //--- Peripheral Test
        protected void Process_Printer_Test()
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : printer test");
            //----------------------------------------------
            if (!CheckAllowRepeatKeyChar((int)KeyPressedType.RECEIPT))
            {
                //LogInfo.WriteLaneEvent($"- Ignore Key PrintReceipt because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                LogInfo.WriteLaneEvent($"- (Maintenance) Ignore Key PrintReceipt because key stroke time less than 2 sec.");
                return;
            }
            //---------------------------------------------
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : print test receipt");
            PrintUtils.PrintReceiptLib.PrintReceipt(0, false, false, "", "", "", true);
        }

        protected void Process_EDC_Test()
        {
            LogInfo.WriteLaneEvent("+ Start Process_EDC_Test");
            if (TCT_Lib.iEdcVersionType == 0)
            {
                var frmEdcPaymentDlg    = new EdcPaymentDlg(1, false);
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                try
                {
                    SetBgIfChanged(frmEdcPaymentDlg, colBk);
                }
                catch (Exception exLog)
                {
                    LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.Process_EDC_Test : " + exLog.Message);
                    Application.Invoke(delegate
                    {
                        SetBgIfChanged(frmEdcPaymentDlg, colBk);
                    });
                }
                finally
                {

                }
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : popup EdcPaymentDlg opened (amount 1, page {TCT_Lib.eCurAppPage})");
                ResponseType xResult = (ResponseType)frmEdcPaymentDlg.Run();
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : popup EdcPaymentDlg closed -> {xResult}");

                if (xResult == ResponseType.Ok)
                {

                }
                //Console.WriteLine("+ After show Response");
                frmEdcPaymentDlg.Dispose();
                frmEdcPaymentDlg.Destroy();
                frmEdcPaymentDlg = null;
            }
            else
            {
                var frmEdcPaymentDlg    = new EdcPaymentDlg_V1(1, false);
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                try
                {
                    SetBgIfChanged(frmEdcPaymentDlg, colBk);
                }
                catch (Exception exLog)
                {
                    LogInfo.WriteLog(LogLevel.ERROR, "- Error Maintenance.Process_EDC_Test : " + exLog.Message);
                    Application.Invoke(delegate
                    {
                        SetBgIfChanged(frmEdcPaymentDlg, colBk);
                    });
                }
                finally
                {

                }
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : popup EdcPaymentDlg_V1 opened (amount 1, page {TCT_Lib.eCurAppPage})");
                ResponseType xResult = (ResponseType)frmEdcPaymentDlg.Run();
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : popup EdcPaymentDlg_V1 closed -> {xResult}");

                if (xResult == ResponseType.Ok)
                {

                }
                //Console.WriteLine("+ After show Response");
                frmEdcPaymentDlg.Dispose();
                frmEdcPaymentDlg.Destroy();
                frmEdcPaymentDlg = null;
            }

            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : page {TCT_Lib.eCurAppPage} -> Maintenance");
            TCT_Lib.eCurAppPage = AppPage.Maintenance;
            LogInfo.WriteLaneEvent("+ End Process_EDC_Test");
        }

        protected void Process_SCR_Test()
        {
            LogInfo.WriteLaneEvent("+ Start Process_SCR_Test");
            bTestModeSCR    = true;
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : SCR test waiting for card (key 0 = exit)");
            szScrName       = "แตะบัตรที่เครื่องอ่าน หรือ กดปุ่ม 0 เพื่อออก";
            btnSubMenu_6.QueueDraw();
        }

        protected void Process_KeyPressed_Test()
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : keyboard/touch test start (key 0 = exit)");
            bTestModeKbd    = true;
            szKeyName       = "กดปุ่มเพื่อทดสอบ หรือ กดปุ่ม 0 เพื่อออก";
            btnSubMenu_6.QueueDraw();
        }

        protected void Process_ForceMode_Test()
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : force mode test -> clear CRS/TFI, DO off");
            TCT_Lib.webSocketLane.OutIo_ClearCRS();
            TCT_Lib.webSocketLane.OutIo_ClearTFI();
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, OFF);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, OFF);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
            TCT_Lib.webSocketLane.OutIo_WriteCRS("", "");
#if !__FULL_SCREEN__
            //--- Sim Device Input LC
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PresenceLoop, OFF);
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageLoop, OFF);
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PresenceOB, OFF);
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageOB, OFF);
#endif
        }

        //--- DSRC Antenna Test
        async protected void Process_MainAntenna_Test()
        {
            bTestDsrcMain = true;
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : main antenna test, enabled {TCT_Lib.bEnableAntenna}, {TCT_Lib.szMainAntenna_IPAddress}:{TCT_Lib.iMainAntenna_SocketPort}");
            if (!TCT_Lib.bEnableAntenna)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, "+ Maintenance : main antenna test skipped, antenna not enabled");
                szDsrcMainName  = "No Enable Main Antenna";
                btnSubMenu_6.QueueDraw();
                await Task.Delay(2000);
                //Thread.Sleep(2000);
                szDsrcMainName  = "";
                btnSubMenu_6.QueueDraw();
            }
            else
            {
                if (TCT_Lib.szMainAntenna_IPAddress == "")
                {
                    LogInfo.WriteLaneEvent(LogLevel.WARN, "+ Maintenance : main antenna test skipped, no IP");
                    szDsrcMainName  = "Not found IP of Main Antenna";
                    btnSubMenu_6.QueueDraw();
                    await Task.Delay(2000);
                    //Thread.Sleep(2000);
                    szDsrcMainName  = "";
                    btnSubMenu_6.QueueDraw();
                }
                else
                {
                    //if (TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szMainAntenna_IPAddress, TCT_Lib.iMainAntenna_SocketPort))
                    if (await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szMainAntenna_IPAddress, TCT_Lib.iMainAntenna_SocketPort))
                        szDsrcMainName = "Success Connect Main Antenna";
                    else
                        szDsrcMainName = "Fail Connect Main Antenna";

                    LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : main antenna result -> " + szDsrcMainName);
                    btnSubMenu_6.QueueDraw();
                    await Task.Delay(2000);
                    //Thread.Sleep(2000);
                    szDsrcMainName  = "";
                    btnSubMenu_6.QueueDraw();
                }
            }

            bTestDsrcMain = false;
        }

        async protected void Process_BackupAntenna_Test()
        {
            bTestDsrcSec = true;
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ Maintenance : secondary antenna test, enabled {TCT_Lib.bEnableAntenna}, {TCT_Lib.szSecondaryAntenna_IPAddress}:{TCT_Lib.iSecondaryAntenna_SocketPort}");
            if (!TCT_Lib.bEnableAntenna)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, "+ Maintenance : secondary antenna test skipped, antenna not enabled");
                szDsrcSecName = "No Enable Secondary Antenna";
                btnSubMenu_6.QueueDraw();
                await Task.Delay(2000);
                //Thread.Sleep(2000);
                szDsrcSecName = "";
                btnSubMenu_6.QueueDraw();
            }
            else
            {
                if (TCT_Lib.szMainAntenna_IPAddress == "")
                {
                    LogInfo.WriteLaneEvent(LogLevel.WARN, "+ Maintenance : secondary antenna test skipped, no IP");
                    szDsrcSecName = "Not found IP of Secondary Antenna";
                    btnSubMenu_6.QueueDraw();
                    await Task.Delay(2000);
                    //Thread.Sleep(2000);
                    szDsrcSecName = "";
                    btnSubMenu_6.QueueDraw();
                }
                else
                {
                    //if (TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szSecondaryAntenna_IPAddress, TCT_Lib.iSecondaryAntenna_SocketPort))
                    if (await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szSecondaryAntenna_IPAddress, TCT_Lib.iSecondaryAntenna_SocketPort))
                        szDsrcSecName = "Success Connect Secondary Antenna";
                    else
                        szDsrcSecName = "Fail Connect Secondary Antenna";

                    LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : secondary antenna result -> " + szDsrcSecName);
                    btnSubMenu_6.QueueDraw();
                    await Task.Delay(2000);
                    //Thread.Sleep(2000);
                    szDsrcSecName = "";
                    btnSubMenu_6.QueueDraw();
                }
            }

            bTestDsrcSec = false;
        }

        //--- Other Test
        protected void Process_Run_Terminal()
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory        = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    FileName                = "/usr/bin/x-terminal-emulator",
                    Arguments               = "",
                    UseShellExecute         = false,
                    RedirectStandardOutput  = true,
                    WindowStyle             = ProcessWindowStyle.Normal
                }
            };

            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : run terminal x-terminal-emulator (wait for exit)");
            proc.Start();
            proc.WaitForExit();
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : terminal closed");
        }

        protected void Action_ShowTestKeyboard(int iKey)
        {
            var keyCode = (KeyPressedType)iKey;
            szKeyName   = keyCode.ToString();
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ Maintenance : keyboard test shows " + szKeyName);
            btnSubMenu_6.QueueDraw();
        }

        //-----------------------------------------------------------
        static DateTime dtLastTimeKeyPrint = DateTime.Now;
        protected bool CheckAllowRepeatKeyChar(int iKey)
        {
            int iSpanMilliSec = TCT_Lib.iKeyStrokeTimeMilliSec;

            if (iKey == (int)KeyPressedType.RECEIPT)    // Key PrintReceipt
            {
                TimeSpan dtTimeSpan = DateTime.Now.Subtract(dtLastTimeKeyPrint);
                dtLastTimeKeyPrint  = DateTime.Now;
                //return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
                return dtTimeSpan.TotalSeconds >= 2;
            }

            return true;
        }

    }
}
