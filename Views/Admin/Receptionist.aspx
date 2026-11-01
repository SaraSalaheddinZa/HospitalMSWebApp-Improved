<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Admin/AdminMaster.Master" AutoEventWireup="true" CodeBehind="Receptionist.aspx.cs" Inherits="HospitalMSWebApp.Views.Admin.Receptionist" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Mybody" runat="server">
    <div class="container-fluid">

        <div class="rec-header">
            <div class="header-text" style="position: relative; z-index: 1">
                <h2>Receptionist Management</h2>
                <p>Manage reception staff accounts and system access</p>
            </div>
            <span class="header-icon"><i class="fas fa-id-badge"></i></span>
            <div style="display: flex; gap: 12px; position: relative; z-index: 1;">
                <div class="header-badge">
                    <div class="badge-num" id="totalReceptionists" runat="server">0</div>
                    <div class="badge-label">Total Staff</div>
                </div>
                <div class="header-badge">
                    <div class="badge-num" id="activeReceptionists" runat="server" style="color: #00c896">0</div>
                    <div class="badge-label">Active</div>
                </div>
            </div>
        </div>

        <div class="stats-strip">
            <div class="stat-pill">
                <div class="stat-icon orange"><i class="fas fa-users"></i></div>
                <div class="stat-info">
                    <div class="num" id="totalReceptionists2" runat="server">0</div>
                    <div class="lbl">Total Receptionists</div>
                </div>
            </div>
            <div class="stat-pill">
                <div class="stat-icon green"><i class="fas fa-circle-check"></i></div>
                <div class="stat-info">
                    <div class="num" id="activeReceptionists2" runat="server">0</div>
                    <div class="lbl">Active Staff</div>
                </div>
            </div>
            <div class="stat-pill">
                <div class="stat-icon blue"><i class="fas fa-calendar-day"></i></div>
                <div class="stat-info">
                    <div class="num" id="todayDate" style="font-size: 1rem; padding-top: 2px"></div>
                    <div class="lbl">Today's Date</div>
                </div>
            </div>
        </div>

        <div class="rec-layout">

            <div class="form-card">
                <div class="form-card-title">
                    <div class="title-icon"><i class="fas fa-plus"></i></div>
                    Staff Details
               
                </div>

                <div id="alertBox" runat="server" class="alert-box"></div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="RecNameTb" runat="server"><i class="fas fa-user"></i>Full Name</asp:Label>
                    <div class="field-wrap">
                        <input type="text" id="RecNameTb" runat="server" placeholder="e.g. Sarah Johnson" />
                        <i class="fas fa-user field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="RecEmailTb" runat="server"><i class="fas fa-envelope"></i>Email Address</asp:Label>
                    <div class="field-wrap">
                        <input type="email" id="RecEmailTb" runat="server" placeholder="staff@hospital.com" />
                        <i class="fas fa-envelope field-ico"></i>
                    </div>
                </div>
                <label><input type="checkbox" name="confirmLoginEmail" /> Confirm changing the existing sign-in email (ends existing sessions).</label>
                <label><input type="checkbox" name="confirmPasswordChange" /> Confirm replacing the selected account's password. Leave password blank to preserve it.</label>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="PhoneTb" runat="server"><i class="fas fa-phone"></i>Phone Number</asp:Label>
                    <div class="field-wrap">
                        <input type="text" id="PhoneTb" runat="server" placeholder="+1 (555) 000-0000" />
                        <i class="fas fa-phone field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="AddressTb" runat="server"><i class="fas fa-location-dot"></i>Address</asp:Label>
                    <div class="field-wrap">
                        <input type="text" id="AddressTb" runat="server" placeholder="Full address" />
                        <i class="fas fa-location-dot field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="PasswordTb" runat="server"><i class="fas fa-lock"></i>Password</asp:Label>
                    <div class="field-wrap">
                        <input type="password" id="PasswordTb" runat="server" autocomplete="new-password" placeholder="Leave blank on edit to preserve password" />
                        <i class="fas fa-lock field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="StatusCb" runat="server"><i class="fas fa-toggle-on"></i>Status</asp:Label>
                    <div class="field-wrap">
                        <asp:DropDownList ID="StatusCb" runat="server" CssClass="">
                            <asp:ListItem Value="Active">Active</asp:ListItem>
                            <asp:ListItem Value="Inactive">Inactive</asp:ListItem>
                        </asp:DropDownList>
                        <i class="fas fa-chevron-down select-arrow"></i>
                    </div>
                </div>

                <div class="divider"></div>

                <div class="btn-row">
                    <asp:Button ID="addBtn" runat="server" Text="＋ Add Staff" CssClass="btn btn-add" OnClick="addBtn_Click" />
                    <asp:Button ID="editBtn" runat="server" Text="✎ Update" CssClass="btn btn-edit" OnClick="editBtn_Click" />
                    <asp:Button ID="deleteBtn" runat="server" Text="✕ Delete" CssClass="btn btn-delete" OnClick="deleteBtn_Click" />
                    <asp:Button ID="clearBtn" runat="server" Text="↺ Clear Form" CssClass="btn btn-clear" OnClick="clearBtn_Click" />
                </div>
            </div>

            <!-- TABLE -->
            <div class="table-card">
                <div class="table-header">
                    <h4 class="table-title">
                        <div class="title-icon"><i class="fas fa-table-list"></i></div>
                        Receptionist Directory
                    </h4>
                    <div class="search-wrap">
                        <i class="fas fa-search"></i>
                        <input type="text" id="searchInput" placeholder="Search by name, email…" onkeyup="filterTable()" />
                    </div>
                </div>

                <div class="table-responsive">
                    <asp:GridView ID="ReceptionistGV" runat="server" CssClass="table gh-table"
                        AutoGenerateColumns="false"
                        OnSelectedIndexChanged="ReceptionistGV_SelectedIndexChanged"
                        DataKeyNames="receptionistID"
                        AllowPaging="true"
                        PageSize="10"
                        OnPageIndexChanging="ReceptionistGV_PageIndexChanging"
                        GridLines="None">
                        <EmptyDataTemplate>
                            <div class="empty-state">
                                <i class="fas fa-users-slash"></i>
                                <p style="font-weight: 600; color: #1e2333; margin: 0 0 4px">No receptionists found</p>
                                <p style="font-size: 0.82rem; margin: 0">Add your first staff member using the form.</p>
                            </div>
                        </EmptyDataTemplate>
                        <Columns>
                            <asp:BoundField DataField="receptionistID" HeaderText="ID" />
                            <asp:BoundField DataField="receptionistName" HeaderText="Name" />
                            <asp:BoundField DataField="receptionistEmail" HeaderText="Email" />
                            <asp:BoundField DataField="receptionistPhone" HeaderText="Phone" />
                            <asp:BoundField DataField="receptionistAdd" HeaderText="Address" />
                            <asp:TemplateField HeaderText="Status">
                                <ItemTemplate>
                                    <span class='badge <%# Eval("receptionistStatus").ToString() == "Active" ? "badge-active" : "badge-inactive" %>'>
                                        <span class="dot"></span><%# Eval("receptionistStatus") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:CommandField ShowSelectButton="true" SelectText="Select" ControlStyle-CssClass="btn-select" />
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>
    </div>

    <script>
        const d = new Date();
        document.getElementById('todayDate').textContent = d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

        function filterTable() {
            const filter = document.getElementById('searchInput').value.toLowerCase();
            const table = document.getElementById('<%= ReceptionistGV.ClientID %>');
            if (!table) return;
            const rows = table.getElementsByTagName('tr');
            for (let i = 1; i < rows.length; i++) {
                const cells = rows[i].getElementsByTagName('td');
                let match = false;
                for (let j = 0; j < cells.length; j++) {
                    if (cells[j].textContent.toLowerCase().includes(filter)) { match = true; break; }
                }
                rows[i].style.display = match ? '' : 'none';
            }
        }

        window.addEventListener('DOMContentLoaded', () => {
            const alert = document.getElementById('<%= alertBox.ClientID %>');
            if (alert && alert.classList.contains('show')) {
                setTimeout(() => alert.classList.remove('show'), 4000);
            }
        });
    </script>
</asp:Content>
