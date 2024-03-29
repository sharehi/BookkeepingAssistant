﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using LibGit2Sharp;

namespace BookkeepingAssistant
{
    public partial class FormInit : Form
    {
        private Repository _repo;
        private bool isInit = false;

        public FormInit()
        {
            InitializeComponent();
        }

        private void FormInit_Load(object sender, EventArgs e)
        {

        }

        private void btnSelectGitDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择数据文件夹。";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtGitRepoDir.Text = dialog.SelectedPath;
            }
            if (!Repository.IsValid(dialog.SelectedPath))
            {
                return;
            }

            _repo = new Repository(dialog.SelectedPath);
            if (_repo.Head.IsTracking)
            {
                txtRemoteUrl.Text = _repo.Network.Remotes[_repo.Head.RemoteName].PushUrl;
            }
            if (_repo.Head.Tip != null)
            {
                txtGitUsername.Text = _repo.Head.Tip.Author.Name;
                txtGitEmail.Text = _repo.Head.Tip.Author.Email;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string gitRepoDir = txtGitRepoDir.Text.Trim();
            if (string.IsNullOrEmpty(gitRepoDir))
            {
                MessageBox.Show("数据文件夹路径不能为空。");
                return;
            }
            string gitRemoteUrl = txtRemoteUrl.Text.Trim();
            if (string.IsNullOrEmpty(gitRemoteUrl))
            {
                MessageBox.Show("Git 仓库推送远程地址不能为空。");
                return;
            }
            string gitUsername = txtGitUsername.Text.Trim();
            if (string.IsNullOrEmpty(gitUsername))
            {
                MessageBox.Show("Git 用户名不能为空。");
                return;
            }
            string gitEmail = txtGitEmail.Text.Trim();
            if (string.IsNullOrEmpty(gitEmail))
            {
                MessageBox.Show("Git 邮箱不能为空。");
                return;
            }

            if (_repo == null)
            {
                if (!Directory.Exists(gitRepoDir))
                {
                    Directory.CreateDirectory(gitRepoDir);
                }
                if (!Repository.IsValid(gitRepoDir))
                {
                    Repository.Init(gitRepoDir);
                }
                _repo = new Repository(gitRepoDir);
            }

            if (_repo.Head.TrackedBranch == null || _repo.Network.Remotes[_repo.Head.RemoteName].PushUrl != gitRemoteUrl)
            {
                AddGitRemote(gitRemoteUrl);
            }

            ConfigModel model = new ConfigModel();
            model.IsInit = true;
            model.GitRepoDir = gitRepoDir;
            model.GitPushUrl = gitRemoteUrl;
            model.GitUsername = gitUsername;
            model.GitEmail = gitEmail;
            ConfigHelper.SaveConfig(model);

            isInit = true;
            _repo.Dispose();
            Close();
        }

        private void AddGitRemote(string url)
        {
            if (_repo.Network.Remotes["origin"] != null)
            {
                _repo.Network.Remotes.Remove("origin");
            }

            Remote remote = _repo.Network.Remotes.Add("origin", url);
            _repo.Branches.Update(_repo.Head,
               b => b.Remote = remote.Name,
               b => b.UpstreamBranch = _repo.Head.CanonicalName);
        }

        private void FormInit_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isInit)
            {
                Environment.Exit(0);
            }
        }
    }
}
