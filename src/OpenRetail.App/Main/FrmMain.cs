﻿/**
 * Copyright (C) 2017 Kamarudin (http://coding4ever.net/)
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * The latest version of this file can be found at https://github.com/rudi-krsoftware/open-retail
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

using OpenRetail.App.Referensi;
using OpenRetail.App.Transaksi;
using OpenRetail.App.Laporan;
using OpenRetail.App.Pengaturan;
using OpenRetail.App.Helper;
using ConceptCave.WaitCursor;
using OpenRetail.Model;
using OpenRetail.Bll.Api;
using OpenRetail.Bll.Service;
using log4net;

namespace OpenRetail.App.Main
{
    public partial class FrmMain : Form, IListener
    {
        //Disable close button
        private const int CP_DISABLE_CLOSE_BUTTON = 0x200;

        private FrmListGolongan _frmListGolongan;
        private FrmListProduk _frmListProduk;
        private FrmListPenyesuaianStok _frmListPenyesuaianStok;

        private FrmListCustomer _frmListCustomer;
        private FrmListSupplier _frmListSupplier;

        private FrmListJabatan _frmListJabatan;
        private FrmListKaryawan _frmListKaryawan;

        private FrmListJenisPengeluaran _frmListJenisPengeluaran;

        private FrmListPembelianProduk _frmListPembelianProduk;
        private FrmListPembayaranHutangPembelianProduk _frmListPembayaranHutangPembelianProduk;
        private FrmListReturPembelianProduk _frmListReturPembelianProduk;

        private FrmListPenjualanProduk _frmListPenjualanProduk;
        private FrmListPembayaranPiutangPenjualanProduk _frmListPembayaranPiutangPenjualanProduk;
        private FrmListReturPenjualanProduk _frmListReturPenjualanProduk;

        private FrmListHakAkses _frmListHakAkses;
        private FrmListOperator _frmListOperator;

        /// <summary>
        /// Variabel lokal untuk menampung menu id. 
        /// Menu id digunakan untuk mengeset hak akses masing-masing form yang diakses
        /// </summary>
        private Dictionary<string, string> _getMenuID;
        private ILog _log;

        public bool IsLogout { get; private set; }

        public FrmMain()
        {
            InitializeComponent();
            mainDock.BackColor = Color.FromArgb(255, 255, 255);

            _log = MainProgram.log;

            AddEventToolbar();
            SetMenuId();
            SetDisabledMenuAndToolbar(menuStrip1, toolStrip1);
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            InitializeStatusBar();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                if (Utils.IsRunningUnderIDE())
                {
                    return base.CreateParams;
                }
                else
                {
                    var cp = base.CreateParams;
                    cp.ClassStyle = cp.ClassStyle | CP_DISABLE_CLOSE_BUTTON;

                    // bug fixed: flicker
                    // http://stackoverflow.com/questions/2612487/how-to-fix-the-flickering-in-user-controls
                    //cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED

                    return cp;
                }
            }
        }

        private void WriteOutput(string s)
        {
            System.Diagnostics.Debug.Print(s);
        }

        private IEnumerable<ToolStripMenuItem> GetItems(ToolStripMenuItem menuItem)
        {
            foreach (var item in menuItem.DropDownItems)
            {
                if (item is ToolStripMenuItem)
                {
                    var dropDownItem = (ToolStripMenuItem)item;

                    if (dropDownItem.HasDropDownItems)
                    {
                        foreach (ToolStripMenuItem subItem in GetItems(dropDownItem))
                            yield return subItem;
                    }

                    yield return (ToolStripMenuItem)item;
                }
            }
        }

        private void SetMenuId()
        {
            IMenuBll menuBll = new MenuBll(_log);
            var listOfMenu = menuBll.GetAll().Where(f => f.parent_id != null && f.nama_form.Length > 0)
                                             .ToList();
            _getMenuID = new Dictionary<string, string>();

            foreach (var item in listOfMenu)
            {
                _getMenuID.Add(item.nama_form, item.menu_id);
            }
        }

        /// <summary>
        /// Method untuk menonaktifkan menu dan toolbar yang belum aktif (membaca setting tabel m_menu)
        /// </summary>
        /// <param name="menuStrip"></param>
        /// <param name="toolStrip"></param>
        private void SetDisabledMenuAndToolbar(MenuStrip menuStrip, ToolStrip toolStrip)
        {
            IMenuBll menuBll = new MenuBll(_log);
            var listOfMenu = menuBll.GetAll()
                                    .Where(f => f.parent_id != null && f.nama_form.Length > 0)
                                    .ToList();
            
            // perulangan untuk mengecek menu dan sub menu
            foreach (ToolStripMenuItem parentMenu in menuStrip.Items)
            {
                var listOfChildMenu = GetItems(parentMenu);

                foreach (var childMenu in listOfChildMenu)
                {
                    var menu = listOfMenu.Where(f => f.nama_menu == childMenu.Name)
                                         .SingleOrDefault();
                    if (menu != null)
                    {
                        childMenu.Enabled = menu.is_enabled;
                    }
                }
            }

            // perulangan untuk mengecek item toolbar
            foreach (ToolStripItem item in toolStrip.Items)
            {
                var menu = listOfMenu.Where(f => f.nama_menu.Substring(3) == item.Name.Substring(2))
                                     .SingleOrDefault();
                if (menu != null)
                {
                    item.Enabled = menu.is_enabled;
                }
            }
        }

        public void InitializeStatusBar()
        {
            var dt = DateTime.Now;

            sbJam.Text = string.Format("{0:HH:mm:ss}", dt);
            sbTanggal.Text = string.Format("{0}, {1}", DayMonthHelper.GetHariIndonesia(dt), dt.Day + " " + DayMonthHelper.GetBulanIndonesia(dt.Month) + " " + dt.Year);

            if (MainProgram.pengguna != null)
                sbOperator.Text = string.Format("Operator : {0}", MainProgram.pengguna.nama_pengguna);

            var firstReleaseYear = 2017;
            var currentYear = DateTime.Today.Year;
            var copyright = currentYear > firstReleaseYear ? string.Format("{0} - {1}", firstReleaseYear, currentYear) : firstReleaseYear.ToString();

            var versi = Utils.GetCurrentVersion("OpenRetail");
            var appName = string.Format(MainProgram.appName, versi, copyright);

            this.Text = appName;
            sbNamaAplikasi.Text = appName.Replace("&", "&&");
        }

        private void AddEventToolbar()
        {
            tbGolongan.Click += mnuGolongan_Click;
            tbProduk.Click += mnuProduk_Click;
            tbPenyesuaianStok.Click += mnuPenyesuaianStok_Click;
            tbSupplier.Click += mnuSupplier_Click;
            tbCustomer.Click += mnuCustomer_Click;
            tbPembelianProduk.Click += mnuPembelianProduk_Click;
            tbPenjualanProduk.Click += mnuPenjualanProduk_Click;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            sbJam.Text = string.Format("{0:HH:mm:ss}", DateTime.Now);
        }

        private bool IsChildFormExists(Form frm)
        {
            return !(frm == null || frm.IsDisposed);
        }

        private void CloseAllDocuments()
        {
            if (this.mainDock.DocumentStyle == DocumentStyle.SystemMdi)
            {
                foreach (var form in MdiChildren)
                    form.Close();
            }
            else
            {
                var documents = this.mainDock.DocumentsToArray();
                foreach (var content in documents)
                    content.DockHandler.Close();
            }
        }

        private string GetMenuTitle(object sender)
        {
            var title = string.Empty;

            if (sender is ToolStripMenuItem)
            {
                title = ((ToolStripMenuItem)sender).Text;
            }
            else
            {
                title = ((ToolStripButton)sender).Text;
            }

            return title;
        }

        private string GetMenuName(object sender)
        {
            var title = string.Empty;

            if (sender is ToolStripMenuItem)
            {
                title = ((ToolStripMenuItem)sender).Name;
            }
            else
            {
                title = ((ToolStripButton)sender).Name;
                title = string.Format("mnu{0}", title.Substring(2));
            }

            return title;
        }

        private string GetFormName(object sender)
        {
            var formName = string.Empty;

            if (sender is ToolStripMenuItem)
            {
                formName = ((ToolStripMenuItem)sender).Tag.ToString();
            }
            else
            {
                formName = ((ToolStripButton)sender).Tag.ToString();
            }

            return formName;
        }

        private void mnuGolongan_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            MsgHelper.MsgInfo(GetMenuName(sender));

            if (!IsChildFormExists(_frmListGolongan))
                _frmListGolongan = new FrmListGolongan(header, MainProgram.pengguna, menuId);

            _frmListGolongan.Show(this.mainDock);
        }

        private void mnuProduk_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListProduk))
                _frmListProduk = new FrmListProduk(header, MainProgram.pengguna, menuId);

            _frmListProduk.Show(this.mainDock);
        }

        private void mnuSupplier_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListSupplier))
                _frmListSupplier = new FrmListSupplier(header, MainProgram.pengguna, menuId);

            _frmListSupplier.Show(this.mainDock);
        }

        private void mnuCustomer_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListCustomer))
                _frmListCustomer = new FrmListCustomer(header, MainProgram.pengguna, menuId);

            _frmListCustomer.Show(this.mainDock);
        }

        private void mnuJabatan_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListJabatan))
                _frmListJabatan = new FrmListJabatan(header, MainProgram.pengguna, menuId);

            _frmListJabatan.Show(this.mainDock);
        }        

        private void mnuPembelianProduk_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListPembelianProduk))
                _frmListPembelianProduk = new FrmListPembelianProduk(header, MainProgram.pengguna, menuId);

            _frmListPembelianProduk.Show(this.mainDock);
        }

        private void mnuJenisPengeluaran_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListJenisPengeluaran))
                _frmListJenisPengeluaran = new FrmListJenisPengeluaran(header, MainProgram.pengguna, menuId);

            _frmListJenisPengeluaran.Show(this.mainDock);
        }

        private void mnuKaryawan_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListKaryawan))
                _frmListKaryawan = new FrmListKaryawan(header, MainProgram.pengguna, menuId);

            _frmListKaryawan.Show(this.mainDock);
        }

        private void mnuPenyesuaianStok_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListPenyesuaianStok))
                _frmListPenyesuaianStok = new FrmListPenyesuaianStok(header, MainProgram.pengguna, menuId);

            _frmListPenyesuaianStok.Show(this.mainDock);
        }

        private void mnuManajemenOperator_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListOperator))
                _frmListOperator = new FrmListOperator(header, MainProgram.pengguna, menuId);

            _frmListOperator.Show(this.mainDock);
        }

        private void mnuHakAksesAplikasi_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListHakAkses))
                _frmListHakAkses = new FrmListHakAkses(header, MainProgram.pengguna, menuId);

            _frmListHakAkses.Show(this.mainDock);
        }

        private void mnuPembayaranHutangPembelianProduk_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListPembayaranHutangPembelianProduk))
                _frmListPembayaranHutangPembelianProduk = new FrmListPembayaranHutangPembelianProduk(header, MainProgram.pengguna, menuId);

            _frmListPembayaranHutangPembelianProduk.Show(this.mainDock);
        }

        private void mnuReturPembelianProduk_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListReturPembelianProduk))
                _frmListReturPembelianProduk = new FrmListReturPembelianProduk(header, MainProgram.pengguna, menuId);

            _frmListReturPembelianProduk.Show(this.mainDock);
        }

        private void mnuPenjualanProduk_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListPenjualanProduk))
                _frmListPenjualanProduk = new FrmListPenjualanProduk(header, MainProgram.pengguna, menuId);

            _frmListPenjualanProduk.Show(this.mainDock);
        }

        private void mnuPembayaranPiutangPenjualanProduk_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListPembayaranPiutangPenjualanProduk))
                _frmListPembayaranPiutangPenjualanProduk = new FrmListPembayaranPiutangPenjualanProduk(header, MainProgram.pengguna, menuId);

            _frmListPembayaranPiutangPenjualanProduk.Show(this.mainDock);
        }

        private void mnuReturPenjualanProduk_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists(_frmListReturPenjualanProduk))
                _frmListReturPenjualanProduk = new FrmListReturPenjualanProduk(header, MainProgram.pengguna, menuId);

            _frmListReturPenjualanProduk.Show(this.mainDock);
        }

        private void mnuProfilPerusahaan_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuName = GetMenuName(sender);

            if (RolePrivilegeHelper.IsHaveHakAkses(menuName, MainProgram.pengguna, GrantState.UPDATE))
            {
                var frmProfil = new FrmProfilPerusahaan(header, MainProgram.profil);
                frmProfil.Listener = this;
                frmProfil.ShowDialog();
            }
            else
                MsgHelper.MsgWarning("Maaf Anda tidak mempunyai otoritas untuk mengakses menu ini");
        }

        public void Ok(object sender, object data)
        {
            if (data is Profil)
            {
                MainProgram.profil = (Profil)data;
                InitializeStatusBar();
            }
        }

        public void Ok(object sender, bool isNewData, object data)
        {
            throw new NotImplementedException();
        }

        private void mnuGantiUser_Click(object sender, EventArgs e)
        {            
            if (MsgHelper.MsgKonfirmasi("Apakah proses ingin dilanjutkan ?"))
            {
                using (new StCursor(Cursors.WaitCursor, new TimeSpan(0, 0, 0, 0)))
                {
                    CloseAllDocuments();

                    this.IsLogout = true;
                    this.Close();
                }
            }
        }

        private void mnuKeluarDariProgram_Click(object sender, EventArgs e)
        {
            if (MsgHelper.MsgKonfirmasi("Apakah proses ingin dilanjutkan ?"))
            {
                using (new StCursor(Cursors.WaitCursor, new TimeSpan(0, 0, 0, 0)))
                {
                    CloseAllDocuments();
                    this.Close();
                }
            }
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            var frmAbout = new FrmAbout();
            frmAbout.ShowDialog();
        }

        private void mnuLapPembelianProduk_Click(object sender, EventArgs e)
        {
            var header = string.Format("Laporan {0}", GetMenuTitle(sender));
            var menuName = GetMenuName(sender);

            if (RolePrivilegeHelper.IsHaveHakAkses(menuName, MainProgram.pengguna, GrantState.SELECT))
            {
                var frmLaporan = new FrmLapPembelianProduk(header);
                frmLaporan.ShowDialog();
            }
            else
                MsgHelper.MsgWarning("Maaf Anda tidak mempunyai otoritas untuk mengakses menu ini");
        }        
    }
}
