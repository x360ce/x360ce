<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CreateUser.ascx.cs"
	Inherits="JocysCom.Web.Security.Controls.CreateUser" %>
<asp:Panel runat="server" ID="ParametersPanel" Visible="false">
	<asp:TextBox runat="server" ID="RedirectUrlTextBox" Width="400px" /><br />
	<asp:TextBox runat="server" ID="ReturnUrlTextBox" Width="400px" />
</asp:Panel>
<div class="SWUI_Panel_Title" id="HeadPanel" runat="server">
	Sign Up
</div>
<div class="SWUI_Panel_Body">
	<div id="no_js_box" style="display: none;">
		<h2>JavaScript is disabled on your browser.</h2>
		<p>
			Please enable JavaScript on your browser or upgrade to a JavaScript-capable browser
				to register for Site.
		</p>
	</div>
	<table class="SWUI_Table" border="0">
		<tr runat="server" id="FirstNameRow">
			<td class="SWUI_Table_Label">
				<label>
					First Name:</label>
			</td>
			<td class="SWUI_Table_Value">
				<asp:TextBox ID="FirstNameTextBox" runat="server" />
			</td>
			<td class="SWUI_Table_Check">
				<div runat="server" id="FirstNameStatus" class="SWUI_Table_Result0">
				</div>
			</td>
		</tr>
		<tr runat="server" id="LastNameRow">
			<td class="SWUI_Table_Label">
				<label>
					Last Name:</label>
			</td>
			<td class="SWUI_Table_Value">
				<asp:TextBox ID="LastNameTextBox" runat="server" />
			</td>
			<td class="SWUI_Table_Check">
				<div runat="server" id="LastNameStatus" class="SWUI_Table_Result0">
				</div>
			</td>
		</tr>
		<tr runat="server" id="EmailRow">
			<td class="SWUI_Table_Label">
				<label>
					Your Email:</label>
			</td>
			<td class="SWUI_Table_Value">
				<asp:TextBox ID="EmailTextBox" runat="server" />
			</td>
			<td class="SWUI_Table_Check">
				<div runat="server" id="EmailStatus" class="SWUI_Table_Result0">
				</div>
			</td>
		</tr>
		<tr runat="server" id="UserNameRow">
			<td class="SWUI_Table_Label">
				<label>
					User Name:</label>
			</td>
			<td class="SWUI_Table_Value">
				<asp:TextBox ID="UserName" runat="server"></asp:TextBox>
			</td>
			<td class="SWUI_Table_Check">
				<div runat="server" id="UserNameStatus" class="SWUI_Table_Result0">
				</div>
			</td>
		</tr>
		<tr runat="server" id="PasswordRow">
			<td class="SWUI_Table_Label">
				<label>
					Password:</label>
			</td>
			<td class="SWUI_Table_Value">
				<asp:TextBox ID="PasswordTextBox" runat="server" TextMode="Password" />
			</td>
			<td class="SWUI_Table_Check">
				<div runat="server" id="PasswordStatus" class="SWUI_Table_Result0">
				</div>
			</td>
		</tr>
		<tr runat="server" id="BirthdayRow">
			<td class="SWUI_Table_Label">
				<label>
					Birthday:</label>
			</td>
			<td class="SWUI_Table_Value">
				<table border="0">
					<tr>
						<td style="padding-right: 8px;">
							<asp:DropDownList ID="YearDropDownList" runat="server">
							</asp:DropDownList>
						</td>
						<td style="padding-right: 8px;">
							<asp:DropDownList ID="MonthDropDownList" runat="server">
							</asp:DropDownList>
						</td>
						<td>
							<asp:DropDownList ID="DayDropDownList" runat="server">
							</asp:DropDownList>
						</td>
					</tr>
				</table>
			</td>
			<td class="SWUI_Table_Check">
				<div runat="server" id="BirthdayStatus" class="SWUI_Table_Result0">
				</div>
			</td>
		</tr>
		<tr runat="server" id="GenderRow">
			<td class="SWUI_Table_Label">
				<label>
					I am:</label>
			</td>
			<td class="SWUI_Table_Value">
				<asp:DropDownList ID="GenderDropDownList" runat="server" ValidationGroup="MemberRegistration">
					<asp:ListItem Text="Select Gender:" Value="" Selected="True" />
					<asp:ListItem Text="Female" Value="F" />
					<asp:ListItem Text="Male" Value="M" />
				</asp:DropDownList>
			</td>
			<td class="SWUI_Table_Check">
				<div runat="server" id="GenderStatus" class="SWUI_Table_Result0">
				</div>
			</td>
		</tr>
		<tr runat="server" id="TermsRow">
			<td class="SWUI_Table_Label"></td>
			<td class="SWUI_Table_Value">
				<table border="0" style="padding-top: 2px;">
					<tr>
						<td style="height: 16px;">
							<asp:CheckBox ID="TermsCheckBox" Text="" runat="server" />
						</td>
						<td style="padding-left: 4px;">
							<label for="<%= TermsCheckBox.ClientID %>">
								I agree to the
							</label>
							<a id="TermsLink" runat="server" href="{0}" target="_blank">terms of use</a>
						</td>
					</tr>
				</table>
			</td>
			<td class="SWUI_Table_Check">
				<div runat="server" id="TermsStatus" class="SWUI_Table_Result0">
				</div>
			</td>
		</tr>
		<tr runat="server" id="NewsRow">
			<td class="SWUI_Table_Label"></td>
			<td class="SWUI_Table_Value">
				<table border="0">
					<tr>
						<td style="height: 16px;">
							<asp:CheckBox ID="NewsCheckBox" runat="server" CssClass="SWUI_Table_CheckBox" />
						</td>
						<td style="padding-left: 4px;">
							<label for="<%= NewsCheckBox.ClientID %>">
								Would you like to receive news<br />
								of our friends' promotions?<span style="display: none;">I would you like to receive
										BookArmy friends News</span></label>
						</td>
					</tr>
				</table>
			</td>
			<td class="SWUI_Table_Check">
				<div runat="server" id="NewsStatus" class="SWUI_Table_Result0" style="display: none;">
				</div>
			</td>
		</tr>
		<tr runat="server" id="SignUpRow">
			<td></td>
			<td>
				<asp:LinkButton runat="server" ID="SignUpLinkButton" OnClick="SignUpLinkButton_Click"
					ValidationGroup="AllMemberRegistration" Text="Sign Up" CssClass="SWUI_Table_Button SWUI_Btn" />
			</td>
			<td></td>
		</tr>
	</table>
	<asp:Panel runat="server" ID="ErrorPanel" CssClass="SWUI_Table_ErrorPanel">
		<asp:Label runat="server" ID="ErrorLabel" />
	</asp:Panel>
	<span style="display: none;">
		<asp:TextBox runat="server" ID="AllTextBox" Text="" /></span>
	<asp:CustomValidator ID="AllCustomValidator" runat="server" ControlToValidate="AllTextBox"
		EnableClientScript="True" Text="All Validator" ValidationGroup="AllMemberRegistration"
		 ErrorMessage="CustomValidator"
		OnServerValidate="AllCustomValidator_ServerValidate" ValidateEmptyText="True"
		Display="Dynamic" />
</div>
<pre runat="server" id="DetailResults" style="display: none;"></pre>

<script type="text/javascript">


	function OpenPassWindow() {
		/// <summary>
		/// Open advanced window with password generator.
		/// </summary>
		var path = System.GetScriptsPath();
		var url = path + "/Examples/System.Security.Password.Frameset.htm?Field=" + passwordTextBox.id;
		// Please note that MSIE 7 forces the presence of the Address Bar by default
		// A missing address bar creates a chance for a fraudster to forge an address of their own.
		// To help thwart that, IE7 will show the address bar on all internet windows to help users see where they are.
		// coming from Microsoft Internet Explorer Blog, Better Website Identification
		// Mozilla.org also intends to soon force the presence of the Location Bar in Firefox 3.
		System.Security.Password.Interface.PassGenWindow = window.open(url, "PassGenWindow", "channelmode=no,directories=no,fullscreen=no,width=850,height=600,location=no,menubar=no,resizable=yes,scrollbars=no,status=yes,titlebar=no,toolbar=no");
		System.Security.Password.Interface.PassGenWindow.focus();
		return false;
	}

</script>

