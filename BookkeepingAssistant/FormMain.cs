﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace BookkeepingAssistant
{
    public partial class FormMain : Form
    {
        private List<TransactionRecordModel> _records;

        public FormMain()
        {
            InitializeComponent();
            txtDate.Text = DateTime.Now.ToString(DAL.TimeFormat);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var config = ConfigHelper.ReadConfig();
            if (!config.IsInit || !DAL.VerifyGitRepo())
            {
                this.Visible = false;
                FormInit formInit = new FormInit();
                formInit.ShowDialog();
                this.Visible = true;
            }

            _records = DAL.Singleton.GetTransactionRecords();
            CleanAndFocusTxtAmount(_records.Any() && _records.First().isIncome);
            txtAmount_TextChanged(null, null);

            RefreshAssetsControl();
            RefreshTransactionTypesControl();
            RefreshDetailView(_records);
        }

        private void CleanAndFocusTxtAmount(bool isPlus)
        {
            if (isPlus)
            {
                txtAmount.Clear();
            }
            else
            {
                txtAmount.Text = "-";
                txtAmount.Select(txtAmount.Text.Length, 0);
            }
            txtAmount.Focus();
        }

        private void RefreshDetailView(List<TransactionRecordModel> records)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("年-月-日");
            dt.Columns.Add("金额", typeof(int));
            dt.Columns.Add("交易类型");
            dt.Columns.Add("资产");
            dt.Columns.Add("交易后该资产余额");
            dt.Columns.Add("交易后所有资产总余额");
            dt.Columns.Add("备注");
            foreach (var item in records)
            {
                DataRow dr = dt.NewRow();
                dr["Id"] = item.Id;
                dr["年-月-日"] = item.Time.ToString(DAL.TimeFormat);
                dr["金额"] = item.Amount;
                dr["交易类型"] = item.TransactionType;
                dr["资产"] = item.AssetName;
                dr["交易后该资产余额"] = item.AssetValue;
                dr["交易后所有资产总余额"] = item.AssetsTotalValue;
                dr["备注"] = item.Remark;
                dt.Rows.Add(dr);
            }
            dgvDetail.DataSource = dt;
            dgvDetail.ClearSelection();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            TransactionRecordModel tr = new TransactionRecordModel();
            decimal amount;
            if (!decimal.TryParse(txtAmount.Text.Trim(), out amount))
            {
                FormMessage.Show("新增失败：金额不能填入非数字。");
                return;
            }
            DateTime time;
            if (!DateTime.TryParse(txtDate.Text.Trim(), out time))
            {
                FormMessage.Show("新增失败：日期格式不正确。正确格式的日期示例：" + DateTime.Now.ToString(DAL.TimeFormat));
                return;
            }

            tr.Amount = amount;
            tr.AssetName = (string)comboBoxAssets.SelectedValue;
            tr.TransactionType = (string)comboBoxTransactionTypes.SelectedValue;
            tr.Time = time;
            tr.Remark = txtRemake.Text.Trim();

            string addResult;
            try
            {
                addResult = DAL.Singleton.AppendTransactionRecord(tr);
            }
            catch (Exception ex)
            {
                FormMessage.Show($"新增交易记录失败：{ ex.Message}。");
                return;
            }
            finally
            {
                RefreshTransactionRecordsView();
            }

            CleanAndFocusTxtAmount(!txtAmount.Text.Trim().StartsWith('-'));
            txtRemake.Clear();
            StringBuilder sbMessage = new StringBuilder();
            sbMessage.AppendLine($"已新增一条{(tr.isIncome ? "收入" : "支出")}：");
            sbMessage.AppendLine($"金额：{tr.Amount}");
            sbMessage.AppendLine($"交易类型：{tr.TransactionType}");
            sbMessage.AppendLine($"资产类型：{tr.AssetName}");
            sbMessage.AppendLine($"交易后余额：{addResult}");
            FormMessage.Show(sbMessage.ToString(), tr.isIncome ? Color.LightGreen : Color.Empty);
        }

        private void RefreshTransactionRecordsView()
        {
            _records = DAL.Singleton.GetTransactionRecords();
            RefreshDetailView(_records);
            RefreshAssetsControl();
        }

        private void linkLabelModifyAssets_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormManageAssets formManageAssets = new FormManageAssets();
            formManageAssets.ShowDialog();
            RefreshAssetsControl();
        }

        private void linkLabelModifyTransactionType_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormManageTransactionType formManageTransactionType = new FormManageTransactionType();
            formManageTransactionType.ShowDialog();
            RefreshTransactionTypesControl();
        }

        private void RefreshAssetsControl()
        {
            var assets = DAL.Singleton.GetDisplayAssets();
            if (!assets.Any())
            {
                comboBoxAssets.DataSource = null;
                return;
            }
            BindingSource bs = new BindingSource();
            bs.DataSource = assets;
            comboBoxAssets.DisplayMember = "Value";
            comboBoxAssets.ValueMember = "Key";
            comboBoxAssets.DataSource = bs;
            if (_records.Any())
            {
                var r = _records.First();
                comboBoxAssets.SelectedValue = r.AssetName;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("【所有资产余额】");
            foreach (var item in assets)
            {
                sb.AppendLine(item.Value);
            }
            sb.AppendLine();
            sb.AppendLine("总余额：" + DAL.Singleton.GetAssets().Values.Sum());
            txtAssets.Text = sb.ToString();
        }

        private void RefreshTransactionTypesControl()
        {
            var types = DAL.Singleton.GetTransactionTypes();
            if (!types.Any())
            {
                comboBoxTransactionTypes.DataSource = null;
                return;
            }
            comboBoxTransactionTypes.DataSource = types;
            if (_records.Any())
            {
                var r = _records.First();
                if (types.Contains(r.TransactionType))
                {
                    comboBoxTransactionTypes.SelectedItem = r.TransactionType;
                }
            }
        }

        private void dgvDetail_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            var row = dgvDetail.Rows[e.RowIndex];
            if ((int)row.Cells["金额"].Value > 0)
            {
                row.DefaultCellStyle.BackColor = Color.LightGreen;
            }
        }

        private void btnRefund_Click(object sender, EventArgs e)
        {
            if (dgvDetail.SelectedRows.Count == 0)
            {
                FormMessage.Show("请先选中一行记录。");
                return;
            }
            int id = (int)dgvDetail.SelectedRows[0].Cells["Id"].Value;
            var record = _records.Single(o => o.Id == id);
            if (record.isIncome)
            {
                FormMessage.Show("收入不可退款。");
                return;
            }
            var linkIds = record.RefundLinkId.Split(',').Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
            var totalRefundAmount = _records.Where(o => linkIds.Contains(o.Id.ToString())).Sum(o => o.Amount);
            if (totalRefundAmount + record.Amount >= 0)
            {
                FormMessage.Show("此支出已全额退款，不可再次退款。");
                return;
            }

            FormRefund formRefund = new FormRefund(Math.Abs(record.Amount) - totalRefundAmount);
            if (formRefund.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            TransactionRecordModel model = new TransactionRecordModel();
            model.AssetName = record.AssetName;
            model.TransactionType = record.TransactionType;
            model.Amount = formRefund.RefundAmount;
            model.RefundLinkId = record.Id.ToString();
            model.Remark = formRefund.RefundAmount >= Math.Abs(record.Amount) ? "[全额退款]" : "[部分退款]";
            try
            {
                DAL.Singleton.AppendTransactionRecord(model);
            }
            catch (Exception ex)
            {
                FormMessage.Show($"新增退款记录失败：{ ex.Message}。");
            }
            finally
            {
                RefreshTransactionRecordsView();
            }
        }

        private void dgvDetail_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDetail.SelectedRows.Count == 0)
            {
                btnDeleteSelect.Enabled = false;
                return;
            }
            btnDeleteSelect.Enabled = true;

            int id = (int)dgvDetail.SelectedRows[0].Cells["Id"].Value;
            var record = _records.Single(o => o.Id == id);
            List<string> excludeTypes = new TransferType[] { TransferType.借款, TransferType.还款,
                TransferType.资产间转账 }.Select(o => o.ToString()).ToList();
            if (record.isIncome || excludeTypes.Contains(record.TransactionType))
            {
                btnRefund.Enabled = false;
            }
            else
            {
                var linkIds = record.RefundLinkId.Split(',').Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
                if (linkIds.Any())
                {
                    var refundRecords = _records.Where(o => linkIds.Contains(o.Id.ToString())).ToList();
                    var totalRefundAmount = refundRecords.Sum(o => o.Amount);
                    btnRefund.Enabled = totalRefundAmount + record.Amount >= 0 ? false : true;

                    new FormRefundRecord(refundRecords)
                    {
                        Text = $"【退款记录】原交易记录 Id：{record.Id}，时间：{record.Time.ToString(DAL.TimeFormat)}，金额：{record.Amount}"
                    }.ShowDialog();
                }
                else
                {
                    btnRefund.Enabled = true;
                }
            }
        }

        private void txtAmount_TextChanged(object sender, EventArgs e)
        {
            if (txtAmount.Text.TrimStart().StartsWith('-'))
            {
                lblInOut.Text = "(支出)";
                lblInOut.BackColor = Color.Transparent;
                txtAmount.BackColor = Color.White;
            }
            else
            {
                lblInOut.Text = "(收入)";
                lblInOut.BackColor = Color.LightGreen;
                txtAmount.BackColor = Color.LightGreen;
            }
        }

        private void txtAmount_Enter(object sender, EventArgs e)
        {
            if (txtAmount.Text.Trim().StartsWith('-'))
            {
                txtAmount.Select(txtAmount.Text.Length, 0);
            }
        }

        private void txtAmount_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                btnAdd.PerformClick();
            }
        }

        private void btnDeleteSelect_Click(object sender, EventArgs e)
        {
            if (dgvDetail.SelectedRows.Count == 0)
            {
                FormMessage.Show("请先选中一行记录。");
                return;
            }
            int id = (int)dgvDetail.SelectedRows[0].Cells["Id"].Value;
            var record = _records.Single(o => o.Id == id);
            if (MessageBox.Show($"Id：{record.Id}，金额：{record.Amount}{Environment.NewLine}确认删除该记录？",
                "确认删除？", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            TransactionRecordModel model = new TransactionRecordModel();
            model.Amount = -record.Amount;
            model.AssetName = record.AssetName;
            model.TransactionType = record.TransactionType;
            model.Remark = "[为删除而抵消]";
            model.DeleteLinkId = record.Id;
            try
            {
                DAL.Singleton.AppendTransactionRecord(model);
            }
            catch (Exception ex)
            {
                FormMessage.Show($"删除记录失败：{ ex.Message}。");
            }
            finally
            {
                RefreshTransactionRecordsView();
            }
        }

        private void btnStatistics_Click(object sender, EventArgs e)
        {
            new FormStatistics(_records).ShowDialog();
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2 || (e.Control && e.KeyCode == Keys.Enter))
            {
                e.Handled = true;
                txtAmount.Focus();
            }
            else if (e.KeyCode == Keys.F8)
            {
                btnStatistics.PerformClick();
            }
            else if (e.KeyCode == Keys.F5)
            {
                dgvDetail.Focus();
            }
        }

        private void btnLoan_Click(object sender, EventArgs e)
        {
            new FormLoan(TransferType.借款).ShowDialog();
            RefreshTransactionRecordsView();
        }

        private void btnRepay_Click(object sender, EventArgs e)
        {
            new FormLoan(TransferType.还款).ShowDialog();
            RefreshTransactionRecordsView();
        }

        private void btnTransfer_Click(object sender, EventArgs e)
        {
            new FormLoan(TransferType.资产间转账).ShowDialog();
            RefreshTransactionRecordsView();
        }
    }
}
