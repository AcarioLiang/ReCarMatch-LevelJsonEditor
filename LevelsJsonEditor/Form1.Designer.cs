
namespace LevelsJsonEditor
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.btnSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.cmbLevelSelect = new System.Windows.Forms.ToolStripComboBox();
            this.btnNewLevel = new System.Windows.Forms.ToolStripButton();
            this.btnDeleteLevel = new System.Windows.Forms.ToolStripButton();
            this.panelTop = new System.Windows.Forms.Panel();
            this.groupBoxLevelInfo = new System.Windows.Forms.GroupBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.groupBoxEntityTree = new System.Windows.Forms.GroupBox();
            this.treeViewEntities = new System.Windows.Forms.TreeView();
            this.groupBoxPreview = new System.Windows.Forms.GroupBox();
            this.panelPreview = new System.Windows.Forms.Panel();
            this.groupBoxProperties = new System.Windows.Forms.GroupBox();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.toolStrip1.SuspendLayout();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.groupBoxEntityTree.SuspendLayout();
            this.groupBoxPreview.SuspendLayout();
            this.groupBoxProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnRefresh,
            this.btnSave,
            this.toolStripSeparator1,
            this.cmbLevelSelect,
            this.btnNewLevel,
            this.btnDeleteLevel});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1428, 33);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnRefresh
            // 
            this.btnRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(45, 30);
            this.btnRefresh.Text = "刷新";
            // 
            // btnSave
            // 
            this.btnSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(45, 30);
            this.btnSave.Text = "保存";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 33);
            // 
            // cmbLevelSelect
            // 
            this.cmbLevelSelect.Name = "cmbLevelSelect";
            this.cmbLevelSelect.Size = new System.Drawing.Size(200, 33);
            this.cmbLevelSelect.Text = "关卡选择";
            // 
            // btnNewLevel
            // 
            this.btnNewLevel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnNewLevel.Name = "btnNewLevel";
            this.btnNewLevel.Size = new System.Drawing.Size(69, 30);
            this.btnNewLevel.Text = "新增关卡";
            // 
            // btnDeleteLevel
            // 
            this.btnDeleteLevel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnDeleteLevel.Name = "btnDeleteLevel";
            this.btnDeleteLevel.Size = new System.Drawing.Size(69, 30);
            this.btnDeleteLevel.Text = "删除关卡";
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.groupBoxLevelInfo);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 33);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1428, 300);
            this.panelTop.TabIndex = 1;
            this.panelTop.AutoSize = true;
            this.panelTop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            // 
            // groupBoxLevelInfo
            // 
            this.groupBoxLevelInfo.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxLevelInfo.Location = new System.Drawing.Point(0, 0);
            this.groupBoxLevelInfo.Name = "groupBoxLevelInfo";
            this.groupBoxLevelInfo.Size = new System.Drawing.Size(1428, 280);
            this.groupBoxLevelInfo.TabIndex = 0;
            this.groupBoxLevelInfo.TabStop = false;
            this.groupBoxLevelInfo.Text = "关卡信息";
            this.groupBoxLevelInfo.AutoSize = true;
            this.groupBoxLevelInfo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 233);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBoxEntityTree);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1428, 961);
            this.splitContainer1.SplitterDistance = 476;
            this.splitContainer1.TabIndex = 2;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.groupBoxPreview);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.groupBoxProperties);
            this.splitContainer2.Size = new System.Drawing.Size(1124, 961);
            this.splitContainer2.SplitterDistance = 476;
            this.splitContainer2.TabIndex = 0;
            // 
            // groupBoxEntityTree
            // 
            this.groupBoxEntityTree.Controls.Add(this.treeViewEntities);
            this.groupBoxEntityTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxEntityTree.Location = new System.Drawing.Point(0, 0);
            this.groupBoxEntityTree.Name = "groupBoxEntityTree";
            this.groupBoxEntityTree.Size = new System.Drawing.Size(300, 961);
            this.groupBoxEntityTree.TabIndex = 0;
            this.groupBoxEntityTree.TabStop = false;
            this.groupBoxEntityTree.Text = "实体树";
            // 
            // treeViewEntities
            // 
            this.treeViewEntities.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewEntities.Location = new System.Drawing.Point(3, 24);
            this.treeViewEntities.Name = "treeViewEntities";
            this.treeViewEntities.Size = new System.Drawing.Size(294, 934);
            this.treeViewEntities.TabIndex = 0;
            // 
            // groupBoxPreview
            // 
            this.groupBoxPreview.Controls.Add(this.panelPreview);
            this.groupBoxPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxPreview.Location = new System.Drawing.Point(0, 0);
            this.groupBoxPreview.Name = "groupBoxPreview";
            this.groupBoxPreview.Size = new System.Drawing.Size(700, 961);
            this.groupBoxPreview.TabIndex = 0;
            this.groupBoxPreview.TabStop = false;
            this.groupBoxPreview.Text = "场景预览";
            // 
            // panelPreview
            // 
            this.panelPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(46)))), ((int)(((byte)(46)))));
            this.panelPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelPreview.Location = new System.Drawing.Point(3, 24);
            this.panelPreview.Name = "panelPreview";
            this.panelPreview.Size = new System.Drawing.Size(694, 934);
            this.panelPreview.TabIndex = 0;
            // 
            // groupBoxProperties
            // 
            this.groupBoxProperties.Controls.Add(this.propertyGrid);
            this.groupBoxProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxProperties.Location = new System.Drawing.Point(0, 0);
            this.groupBoxProperties.Name = "groupBoxProperties";
            this.groupBoxProperties.Size = new System.Drawing.Size(420, 961);
            this.groupBoxProperties.TabIndex = 0;
            this.groupBoxProperties.TabStop = false;
            this.groupBoxProperties.Text = "属性编辑";
            // 
            // propertyGrid
            // 
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid.Location = new System.Drawing.Point(3, 24);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(414, 934);
            this.propertyGrid.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1428, 1194);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Form1";
            this.Text = "Levels JSON 编辑器";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panelTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.groupBoxEntityTree.ResumeLayout(false);
            this.groupBoxPreview.ResumeLayout(false);
            this.groupBoxProperties.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnRefresh;
        private System.Windows.Forms.ToolStripButton btnSave;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripComboBox cmbLevelSelect;
        private System.Windows.Forms.ToolStripButton btnNewLevel;
        private System.Windows.Forms.ToolStripButton btnDeleteLevel;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.GroupBox groupBoxLevelInfo;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.GroupBox groupBoxEntityTree;
        private System.Windows.Forms.TreeView treeViewEntities;
        private System.Windows.Forms.GroupBox groupBoxPreview;
        private System.Windows.Forms.Panel panelPreview;
        private System.Windows.Forms.GroupBox groupBoxProperties;
        private System.Windows.Forms.PropertyGrid propertyGrid;
    }
}

