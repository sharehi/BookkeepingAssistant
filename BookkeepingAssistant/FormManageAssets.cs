﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace BookkeepingAssistant
{
    public partial class FormManageAssets : Form
    {
        public FormManageAssets()
        {
            InitializeComponent();
        }

        private void FormManageAssets_Load(object sender, EventArgs e)
        {
            DisplayAssets();
        }

        private void btnAddAsset_Click(object sender, EventArgs e)
        {
            string assetName = txtAssetName.Text.Trim();
            if (string.IsNullOrEmpty(assetName))
            {
                FormMessage.Show("新增失败：名称不能为空。");
                return;
            }
            if (DAL.Singleton.GetAssets().ContainsKey(assetName))
            {
                FormMessage.Show("新增失败：已存在该名称的资产。");
                return;
            }

            decimal assetValue;
            if (!decimal.TryParse(txtAssetValue.Text.Trim(), out assetValue))
            {
                FormMessage.Show("新增失败：资产余额不能填非数字。");
                return;
            }

            DAL.Singleton.AddAsset(assetName, assetValue);
            DisplayAssets();
            FormMessage.Show($"已新增「{assetName}」");
            txtAssetName.Clear();
            txtAssetValue.Clear();
            txtAssetName.Focus();
        }

        private void DisplayAssets()
        {
            var assets = DAL.Singleton.GetDisplayAssets();
            if (!assets.Any())
            {
                btnRemove.Enabled = false;
                comboBoxAssets.DataSource = null;
                return;
            }

            BindingSource bs = new BindingSource();
            bs.DataSource = assets;
            comboBoxAssets.DisplayMember = "Value";
            comboBoxAssets.ValueMember = "Key";
            comboBoxAssets.DataSource = bs;
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            string assetName = (string)comboBoxAssets.SelectedValue;
            if (MessageBox.Show($"确认删除{assetName}？", "确认删除？", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                DAL.Singleton.RemoveAsset(assetName);
            }
            catch (Exception ex)
            {
                FormMessage.Show("删除失败：" + ex.Message);
            }
            DisplayAssets();
        }
    }
}
