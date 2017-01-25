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
using OpenRetail.App.Helper;

namespace OpenRetail.App.UI.Template
{
    /// <summary>
    /// Base class form untuk menampilkan data
    /// </summary>
    public partial class FrmListStandard : BaseFrmList
    {
        public FrmListStandard()
        {
            InitializeComponent();
            ColorManagerHelper.SetTheme(this, this);
        }

        public FrmListStandard(string header) 
            : this()
        {
            this.Text = header;
            this.TabText = header;
            this.lblHeader.Text = header;
        }

        protected override void SetActiveBtnPerbaikiAndHapus(bool status)
        {
            btnPerbaiki.Enabled = status;
            btnHapus.Enabled = status;
        }

        private void btnTambah_Click(object sender, EventArgs e)
        {
            Tambah();
        }

        private void btnPerbaiki_Click(object sender, EventArgs e)
        {
            Perbaiki();
        }

        private void btnHapus_Click(object sender, EventArgs e)
        {
            Hapus();
        }

        private void gridList_DoubleClick(object sender, EventArgs e)
        {
            if (btnPerbaiki.Enabled)
                Perbaiki();
        }        

        private void btnSelesai_Click(object sender, EventArgs e)
        {
            Selesai();
        }
    }
}
