<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Receptionist/ReceptionistMaster.Master" AutoEventWireup="true" CodeBehind="Patients.aspx.cs" Inherits="HospitalMSWebApp.Views.Receptionist.Patients" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MyBody" runat="server">
    <div class="container-fluid">

        <!-- Header -->
        <div class="pat-header">
            <div class="header-text">
                <h2>Patient Management</h2>
                <p>Register, update and manage patient records</p>
            </div>
            <span class="header-icon"><i class="fas fa-user-injured"></i></span>
            <div class="header-badges">
                <div class="header-badge">
                    <div class="badge-num" id="totalPatients" runat="server">0</div>
                    <div class="badge-label">Total</div>
                </div>
                <div class="header-badge">
                    <div class="badge-num green" id="todayPatients" runat="server">0</div>
                    <div class="badge-label">Today</div>
                </div>
            </div>
        </div>

        <div class="pat-layout">

            <!-- FORM -->
            <div class="form-card">
                <div class="form-card-title">
                    <div class="title-icon cyan"><i class="fas fa-user-plus"></i></div>
                    Patient Details
               
                </div>

                <div id="alertBox" runat="server" class="alert-box"></div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="PatNameTb" runat="server"><i class="fas fa-user"></i>Full Name</asp:Label>
                    <div class="field-wrap">
                        <input type="text" id="PatNameTb" runat="server" placeholder="Patient full name" />
                        <i class="fas fa-user field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="PhoneTb" runat="server"><i class="fas fa-phone"></i>Phone</asp:Label>
                    <div class="field-wrap">
                        <input type="text" inputmode="tel" id="PhoneTb" runat="server" placeholder="Include country code" />
                        <i class="fas fa-phone field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="GenderCb" runat="server"><i class="fas fa-venus-mars"></i>Gender</asp:Label>
                    <div class="field-wrap no-ico">
                        <asp:DropDownList ID="GenderCb" runat="server">
                            <asp:ListItem Value="">— Select —</asp:ListItem>
                            <asp:ListItem Value="Female">Female</asp:ListItem>
                            <asp:ListItem Value="Male">Male</asp:ListItem>
                            <asp:ListItem Value="Other">Other</asp:ListItem>
                        </asp:DropDownList>
                        <i class="fas fa-chevron-down select-arr"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="DOBTb" runat="server"><i class="fas fa-cake-candles"></i>Date of Birth</asp:Label>
                    <div class="field-wrap no-ico">
                        <input type="date" id="DOBTb" runat="server" />
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
                    <asp:Label CssClass="field-label" AssociatedControlID="AllergyTb" runat="server"><i class="fas fa-triangle-exclamation"></i>Allergies</asp:Label>
                    <div class="field-wrap">
                        <input type="text" id="AllergyTb" runat="server" placeholder="e.g. Penicillin, Peanuts" />
                        <i class="fas fa-triangle-exclamation field-ico"></i>
                    </div>
                </div>

                <div class="divider"></div>

                <div class="btn-row">
                    <asp:Button ID="addBtn" runat="server" Text="＋ Register Patient" CssClass="btn btn-add" OnClick="addBtn_Click" />
                    <asp:Button ID="editBtn" runat="server" Text="✎ Update" CssClass="btn btn-edit" OnClick="editBtn_Click" />
                    <asp:Button ID="deleteBtn" runat="server" Text="Deactivate" CssClass="btn btn-delete" OnClick="deleteBtn_Click" OnClientClick="return confirm('Deactivate this patient record? Historical clinical records will be retained.');" />
                    <asp:Button ID="clearBtn" runat="server" Text="↺ Clear Form" CssClass="btn btn-clear" OnClick="clearBtn_Click" />
                </div>
            </div>

            <!-- TABLE -->
            <div class="table-card">
                <div class="table-header">
                    <h4 class="table-title">
                        <div class="title-icon blue"><i class="fas fa-table-list"></i></div>
                        Patient Directory
                    </h4>
                    <div class="search-wrap">
                        <i class="fas fa-search"></i>
                        <input type="search" id="searchInput" aria-label="Search patients on this page" placeholder="Search this page…" onkeyup="filterTable()" />
                    </div>
                </div>

                <div class="table-responsive">
                    <asp:GridView ID="PatientListGV" runat="server" CssClass="table gh-table"
                        AutoGenerateColumns="false"
                        OnSelectedIndexChanged="PatientListGV_SelectedIndexChanged"
                        DataKeyNames="patientId"
                        AllowPaging="true"
                        PageSize="10"
                        OnPageIndexChanging="PatientListGV_PageIndexChanging"
                        GridLines="None">
                        <EmptyDataTemplate>
                            <div class="empty-state">
                                <i class="fas fa-user-injured"></i>
                                <p style="font-weight: 600; color: #1e2333; margin: 0 0 4px">No patients registered</p>
                                <p style="font-size: 0.82rem; margin: 0">Use the form to register the first patient.</p>
                            </div>
                        </EmptyDataTemplate>
                        <Columns>
                            <asp:BoundField DataField="MedicalRecordNumber" HeaderText="MRN" />
                            <asp:BoundField DataField="patientName" HeaderText="Name" />
                            <asp:BoundField DataField="patientPhone" HeaderText="Phone" />
                            <asp:TemplateField HeaderText="Gender">
                                <ItemTemplate>
                                    <span class='gender-badge <%# GetGenderClass(Eval("patientGender").ToString()) %>'>
                                        <%#: Eval("patientGender") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="patientDOB" HeaderText="DOB" DataFormatString="{0:yyyy-MM-dd}" />
                            <asp:BoundField DataField="patientAdd" HeaderText="Address" />
                            <asp:TemplateField HeaderText="Allergies">
                                <ItemTemplate>
                                    <span class="allergy-chip"><%#: string.IsNullOrWhiteSpace(Convert.ToString(Eval("patientAllergies"))) ? "Not recorded" : Convert.ToString(Eval("patientAllergies")) %></span>
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
        function filterTable() {
            const filter = document.getElementById('searchInput').value.toLowerCase();
            const table = document.getElementById('<%= PatientListGV.ClientID %>');
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
    </script>
</asp:Content>
