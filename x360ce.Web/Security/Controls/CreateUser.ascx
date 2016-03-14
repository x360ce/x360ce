<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CreateUser.ascx.cs"
	Inherits="JocysCom.Web.Security.Controls.CreateUser" %>
<asp:Panel runat="server" ID="ParametersPanel" Visible="false">
	<asp:TextBox runat="server" ID="RedirectUrlTextBox" Width="400px" /><br />
	<asp:TextBox runat="server" ID="ReturnUrlTextBox" Width="400px" />
</asp:Panel>
<div class="Login_Title" id="HeadPanel" runat="server">
	Sign Up</div>
<div class="Login_Body">
		<div id="no_js_box" style="display: none;">
			<h2>
				JavaScript is disabled on your browser.</h2>
			<p>
				Please enable JavaScript on your browser or upgrade to a JavaScript-capable browser
				to register for Site.</p>
		</div>
		<table class="BA_FormTable" border="0">
			<tr runat="server" id="FirstNameRow">
				<td class="BA_FormTable_Label">
					<label>
						First Name:</label>
				</td>
				<td class="BA_FormTable_Value">
					<asp:TextBox ID="FirstNameTextBox" runat="server" />
				</td>
				<td class="BA_FormTable_Check">
					<div runat="server" id="FirstNameStatus" class="BA_FormTable_Result0">
					</div>
				</td>
			</tr>
			<tr runat="server" id="LastNameRow">
				<td class="BA_FormTable_Label">
					<label>
						Last Name:</label>
				</td>
				<td class="BA_FormTable_Value">
					<asp:TextBox ID="LastNameTextBox" runat="server" />
				</td>
				<td class="BA_FormTable_Check">
					<div runat="server" id="LastNameStatus" class="BA_FormTable_Result0">
					</div>
				</td>
			</tr>
			<tr runat="server" id="EmailRow">
				<td class="BA_FormTable_Label">
					<label>
						Your Email:</label>
				</td>
				<td class="BA_FormTable_Value">
					<asp:TextBox ID="EmailTextBox" runat="server" />
				</td>
				<td class="BA_FormTable_Check">
					<div runat="server" id="EmailStatus" class="BA_FormTable_Result0">
					</div>
				</td>
			</tr>
			<tr runat="server" id="UsernameRow">
				<td class="BA_FormTable_Label">
					<label>
						User Name:</label>
				</td>
				<td class="BA_FormTable_Value">
					<asp:TextBox ID="UsernameTextBox" runat="server"></asp:TextBox>
				</td>
				<td class="BA_FormTable_Check">
					<div runat="server" id="UsernameStatus" class="BA_FormTable_Result0">
					</div>
				</td>
			</tr>
			<tr runat="server" id="PasswordRow">
				<td class="BA_FormTable_Label">
					<label>
						Password:</label>
				</td>
				<td class="BA_FormTable_Value">
					<asp:TextBox ID="PasswordTextBox" runat="server" TextMode="Password" />
				</td>
				<td class="BA_FormTable_Check">
					<div runat="server" id="PasswordStatus" class="BA_FormTable_Result0">
					</div>
				</td>
			</tr>
			<tr runat="server" id="BirthdayRow">
				<td class="BA_FormTable_Label">
					<label>
						Birthday:</label>
				</td>
				<td class="BA_FormTable_Value">
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
				<td class="BA_FormTable_Check">
					<div runat="server" id="BirthdayStatus" class="BA_FormTable_Result0">
					</div>
				</td>
			</tr>
			<tr runat="server" id="GenderRow">
				<td class="BA_FormTable_Label">
					<label>
						I am:</label>
				</td>
				<td class="BA_FormTable_Value">
					<asp:DropDownList ID="GenderDropDownList" runat="server" ValidationGroup="MemberRegistration">
						<asp:ListItem Text="Select Gender:" Value="" Selected="True" />
						<asp:ListItem Text="Female" Value="F" />
						<asp:ListItem Text="Male" Value="M" />
					</asp:DropDownList>
				</td>
				<td class="BA_FormTable_Check">
					<div runat="server" id="GenderStatus" class="BA_FormTable_Result0">
					</div>
				</td>
			</tr>
			<tr runat="server" id="TermsRow">
				<td class="BA_FormTable_Label">
				</td>
				<td class="BA_FormTable_Value">
					<table border="0" style="padding-top: 2px;">
						<tr>
							<td style="height: 16px;">
								<asp:CheckBox ID="TermsCheckBox" Text="" runat="server" onclick="Profile.RequestServerValidation(this, null)" />
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
				<td class="BA_FormTable_Check">
					<div runat="server" id="TermsStatus" class="BA_FormTable_Result0">
					</div>
				</td>
			</tr>
			<tr runat="server" id="NewsRow">
				<td class="BA_FormTable_Label">
				</td>
				<td class="BA_FormTable_Value">
					<table border="0">
						<tr>
							<td style="height: 16px;">
								<asp:CheckBox ID="NewsCheckBox" runat="server" CssClass="BA_FormTable_CheckBox" onclick="Profile.RequestServerValidation(this, null)" />
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
				<td class="BA_FormTable_Check">
					<div runat="server" id="NewsStatus" class="BA_FormTable_Result0" style="display: none;">
					</div>
				</td>
			</tr>
			<tr runat="server" id="SignUpRow">
				<td>
				</td>
				<td>
					<asp:Button runat="server" ID="ClientLinkButton" CssClass="SWUI_Prg Login_Button"
						Text="Sign Up" OnClientClick="return false;" />
					<asp:LinkButton runat="server" ID="SignUpLinkButton" OnClick="SignUpLinkButton_Click"
						ValidationGroup="AllMemberRegistration" Style="display: none;">Sign Up (server)</asp:LinkButton>
				</td>
				<td>
				</td>
			</tr>
		</table>
	<asp:Panel runat="server" ID="ErrorPanel" CssClass="BA_FormTable_ErrorPanel" Style="display: none;">
	</asp:Panel>
	<asp:Label runat="server" ID="ErrorPanelLabel" />
	<asp:CustomValidator ID="FirstNameTextBoxCustomValidator" runat="server" ControlToValidate="FirstNameTextBox"
		Text="FirsName Validator" ValidateEmptyText="true" ValidationGroup="MemberRegistration"
		EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="LastNameTextBoxCustomValidator" runat="server" ControlToValidate="LastNameTextBox"
		Text="LastName Validator" ValidateEmptyText="true" ValidationGroup="MemberRegistration"
		EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="EmailTextBoxCustomValidator" runat="server" Text="Email Validator"
		ControlToValidate="EmailTextBox" ValidateEmptyText="true" ValidationGroup="MemberRegistration"
		EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="UsernameTextBoxCustomValidator" runat="server" Text="User Name Validator"
		ControlToValidate="UsernameTextBox" ValidateEmptyText="true" ValidationGroup="MemberRegistration"
		EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="PasswordTextBoxCustomValidator" runat="server" Text="Password Validator"
		ControlToValidate="PasswordTextBox" ValidateEmptyText="true" ValidationGroup="MemberRegistration"
		EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="BirthdayYearDropDownListCustomValidator" runat="server"
		Text="Birthday Year Validator" ValidateEmptyText="true" ValidationGroup="MemberRegistration"
		ControlToValidate="YearDropDownList" EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="BirthdayMonthDropDownListCustomValidator" runat="server"
		Text="Birthday Month Validator" ValidateEmptyText="true" ValidationGroup="MemberRegistration"
		ControlToValidate="MonthDropDownList" EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="BirthdayDayDropDownListCustomValidator" runat="server" Text="Birthday Day Validator"
		ValidateEmptyText="true" ValidationGroup="MemberRegistration" ControlToValidate="DayDropDownList"
		EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="GenderDropDownListCustomValidator" runat="server" ControlToValidate="GenderDropDownList"
		Text="Gender Validator" ValidateEmptyText="true" ValidationGroup="MemberRegistration"
		EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="TermsCheckBoxCustomValidator" runat="server" Text="Terms Validator"
		ValidationGroup="MemberRegistration" EnableClientScript="true" ClientValidationFunction="Profile.RequestServerValidation"
		Display="Dynamic" />
	<asp:CustomValidator ID="NewsCheckBoxCustomValidator" runat="server" Text="News Validator"
		ValidationGroup="MemberRegistration" EnableClientScript="true" Display="Dynamic" />
	<span style="display: none;">
		<asp:TextBox runat="server" ID="AllTextBox" Text="" /></span>
	<asp:CustomValidator ID="AllCustomValidator" runat="server" ControlToValidate="AllTextBox"
		EnableClientScript="True" Text="All Validator" ValidationGroup="AllMemberRegistration"
		ClientValidationFunction="Profile.AllCustomValidator_ClientValidate" ErrorMessage="CustomValidator"
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

	var Profile = {};

	Profile.Window_Load = function() {
		for (var property in Profile.Controls) {
			var control = Profile.Controls[property];
			if (control.id.indexOf("TextBox") == -1) continue;
			Sys.UI.DomEvent.addHandler(control, 'blur',
				function() { Profile.RequestServerValidation(control, null); }
			);
		}
		Sys.UI.DomEvent.addHandler(Profile.Controls.ClientLinkButton, 'click', Profile.SignUp_OnClientClick);
	}

	Profile.SignUpClicked = false;

	Profile.SignUp_OnClientClick = function() {
		for (var property in Profile.Changed) {
			Profile.Changed[property] = true;
		}
		Profile.SignUpClicked = true;
		Profile.RequestServerValidation();
	}

	Profile.Controls = {};
	Profile.Controls.FirstName = document.getElementById("<%=FirstNameTextBox.ClientID%>");
	Profile.Controls.LastName = document.getElementById("<%=LastNameTextBox.ClientID%>");
	Profile.Controls.Email = document.getElementById("<%=EmailTextBox.ClientID%>");
	Profile.Controls.Username = document.getElementById("<%=UsernameTextBox.ClientID%>");
	Profile.Controls.Password = document.getElementById("<%=PasswordTextBox.ClientID%>");
	Profile.Controls.BirthdayYear = document.getElementById("<%=YearDropDownList.ClientID%>");
	Profile.Controls.BirthdayMonth = document.getElementById("<%=MonthDropDownList.ClientID%>");
	Profile.Controls.BirthdayDay = document.getElementById("<%=DayDropDownList.ClientID%>");
	Profile.Controls.Gender = document.getElementById("<%=GenderDropDownList.ClientID%>");
	Profile.Controls.Terms = document.getElementById("<%=TermsCheckBox.ClientID%>");
	Profile.Controls.News = document.getElementById("<%=NewsCheckBox.ClientID%>");

	Profile.Controls.ErrorPanel = document.getElementById("<%=ErrorPanel.ClientID%>");
	Profile.Controls.DetailResults = document.getElementById("<%=DetailResults.ClientID%>");
	Profile.Controls.SignUpLinkButton = document.getElementById("<%=SignUpLinkButton.ClientID%>");
	Profile.Controls.ClientLinkButton = document.getElementById("<%=ClientLinkButton.ClientID%>");
	Profile.Controls.AllCustomValidator = document.getElementById("<%=AllCustomValidator.ClientID%>");

	Profile.Validators = {};
	Profile.Validators.FirstName = document.getElementById("<%=FirstNameTextBoxCustomValidator.ClientID%>");
	Profile.Validators.LastName = document.getElementById("<%=LastNameTextBoxCustomValidator.ClientID%>");
	Profile.Validators.Email = document.getElementById("<%=EmailTextBoxCustomValidator.ClientID%>");
	Profile.Validators.Username = document.getElementById("<%=UsernameTextBoxCustomValidator.ClientID%>");
	Profile.Validators.Password = document.getElementById("<%=PasswordTextBoxCustomValidator.ClientID%>");
	Profile.Validators.BirthdayYear = document.getElementById("<%=BirthdayYearDropDownListCustomValidator.ClientID%>");
	Profile.Validators.BirthdayMonth = document.getElementById("<%=BirthdayMonthDropDownListCustomValidator.ClientID%>");
	Profile.Validators.BirthdayDay = document.getElementById("<%=BirthdayDayDropDownListCustomValidator.ClientID%>");
	Profile.Validators.Gender = document.getElementById("<%=GenderDropDownListCustomValidator.ClientID%>");
	Profile.Validators.Terms = document.getElementById("<%=TermsCheckBoxCustomValidator.ClientID%>");
	Profile.Validators.News = document.getElementById("<%=NewsCheckBoxCustomValidator.ClientID%>");

	Profile.Status = {};
	Profile.Status.FirstName = document.getElementById("<%=FirstNameStatus.ClientID%>");
	Profile.Status.LastName = document.getElementById("<%=LastNameStatus.ClientID%>");
	Profile.Status.Email = document.getElementById("<%=EmailStatus.ClientID%>");
	Profile.Status.Username = document.getElementById("<%=UsernameStatus.ClientID%>");
	Profile.Status.Password = document.getElementById("<%=PasswordStatus.ClientID%>");
	Profile.Status.Birthday = document.getElementById("<%=BirthdayStatus.ClientID%>");
	Profile.Status.Gender = document.getElementById("<%=GenderStatus.ClientID%>");
	Profile.Status.Terms = document.getElementById("<%=TermsStatus.ClientID%>");
	Profile.Status.News = document.getElementById("<%=NewsStatus.ClientID%>");

	Profile.Rows = {};
	Profile.Rows.FirstName = document.getElementById("<%=FirstNameRow.ClientID%>");
	Profile.Rows.LastName = document.getElementById("<%=LastNameRow.ClientID%>");
	Profile.Rows.Email = document.getElementById("<%=EmailRow.ClientID%>");
	Profile.Rows.Username = document.getElementById("<%=UsernameRow.ClientID%>");
	Profile.Rows.Password = document.getElementById("<%=PasswordRow.ClientID%>");
	Profile.Rows.Birthday = document.getElementById("<%=BirthdayRow.ClientID%>");
	Profile.Rows.Gender = document.getElementById("<%=GenderRow.ClientID%>");
	Profile.Rows.Terms = document.getElementById("<%=TermsRow.ClientID%>");
	Profile.Rows.News = document.getElementById("<%=NewsRow.ClientID%>");

	// Checks if field was changed by user.
	Profile.Changed = {};
	Profile.Changed.FirstName = false;
	Profile.Changed.LastName = false;
	Profile.Changed.Email = false;
	Profile.Changed.Username = false;
	Profile.Changed.Password = false;
	Profile.Changed.Birthday = false;
	Profile.Changed.Gender = false;
	Profile.Changed.Terms = false;
	Profile.Changed.News = false;

	Profile.RequestServerValidation = function(sender, e) {
		// Mark property as changed.
		if (sender != null) {
			for (var property in Profile.Changed) {
				if (sender.id.indexOf(property) > -1) {
					Profile.Changed[property] = true;
				}
			}
			//var control = $get(sender.controltovalidate)
		}

		if (Profile.Validators.Password.enabled == undefined)
			Profile.Validators.Password.enabled = true;

		// /WebServices/ServerInfo.asmx/js
		JocysCom.WebSites.WebApp.WebServices.ServerInfo.ValidateUserRegistration(
			Profile.Controls.FirstName.value,
			Profile.Controls.LastName.value,
			Profile.Controls.Email.value,
			Profile.Controls.Username.value,
			Profile.Validators.Password.enabled ? Profile.Controls.Password.value : 'automatic',
			Profile.Controls.BirthdayYear.value + "-" + Profile.Controls.BirthdayMonth.value + "-" + Profile.Controls.BirthdayDay.value,
			Profile.Controls.Gender.value,
			Profile.Controls.Terms.checked,
			Profile.Controls.News.checked,
			Profile.RequestServerValidation_Success,
			Profile.RequestServerValidation_Failed,
			this
		)
	}

	PropertiesToString = function(object) {
		/// <summary>
		/// 
		/// </summary>
		/// <returns type="String" />
		var results = new String();
		results += typeof (object) + " properties:\r\n";
		for (var property in object) {
			var valueType = typeof (object[property]);
			var value = object[property];
			results += valueType + " " + property + " = " + value + "\r\n";
		}
		return results;
	}

	Profile.AllCustomValidator_ClientValidate = function(sender, e) {
		// Check validators.
		var validator;
		for (var property in Profile.Validators) {
			validator = Profile.Validators[property];
			// If one of validators is not valid then exit.
			if (!validator.IsValid) {
				break;
			}
		}
		// Assign value if this function is called from validator.
		if (e) {
			e.IsValid = validator.IsValid;
			Profile.Controls.AllCustomValidator.innerHTML = "All Validator:<br />" + validator.id + " " + validator.IsValid + "<br />" + (new Date);
		}
		// Return last validator checked.
		return { IsValid: validator.IsValid, Id: validator.id };
	}

	Profile.Calls = 0;

	Profile.RequestServerValidation_Success = function(result) {
		var s = "";
		Profile.Calls++;
		for (var i = 0; i < result.length; i++) {
			s += Profile.Calls + " " + new Date() + "\r\n";
			s += "-------------------------\r\n"
			s += "IsValid = " + (result[i].Message.length == 0) + "\r\n";
			s += PropertiesToString(result[i]);
			var name = result[i].Name;
			var control = Profile.Controls[name];
			var status = Profile.Status[name];
			status.className = result[i].Message.length == 0
				? "BA_FormTable_Result1"
				: "BA_FormTable_Result0" + (Profile.Changed[name] ? "Changed" : "");
		}
		Profile.Controls.ErrorPanel.style.display = "";
		// Because we are using web services
		// we need to update validators from result now.
		for (var i = 0; i < result.length; i++) {
			var name = result[i].Name;
			var isValid = result[i].Message.length == 0;
			if (name == "Birthday") {
				Profile.Validators[name + "Year"].IsValid = isValid;
				Profile.Validators[name + "Month"].IsValid = isValid;
				Profile.Validators[name + "Day"].IsValid = isValid;
			} else {
				Profile.Validators[name].IsValid = isValid;
			}
		}
		// Assign validation result to global validator
		for (var i = 0; i < result.length; i++) {
			var msg = result[i].Message;
			var name = result[i].Name;
			if (msg.length > 0 && Profile.Changed[name]) {
				Profile.Controls.ErrorPanel.innerHTML = msg;
				Profile.Controls.ErrorPanel.style.display = "";
				// Show row with error.
				Profile.Rows[name].style.display = "";
				break;
			}
			Profile.Controls.ErrorPanel.innerHTML = "";
			Profile.Controls.ErrorPanel.style.display = "none";
		}

		Profile.Controls.DetailResults.innerHTML = s;
		if (Profile.SignUpClicked) {
			Profile.SignUpClicked = false;
			var all = Profile.AllCustomValidator_ClientValidate(null, null);
			//alert(all.IsValid + " " + all.Id);
			if (all.IsValid) {
				// Prevent multi clicks.
				Sys.UI.DomEvent.removeHandler(Profile.Controls.ClientLinkButton, 'click', Profile.SignUp_OnClientClick);
				Profile.Controls.ClientLinkButton.className = "BA_FormTable_Button_Disabled";
				// Create click() function if not exists.
				var b = Profile.Controls.SignUpLinkButton;
				if (b && typeof (b.click) == 'undefined') {
					b.click = function() {
						var result = true;
						if (b.onclick) result = b.onclick();
						if (typeof (result) == 'undefined' || result) {
							eval(b.getAttribute('href'));
						}
					}
				}
				// Click SignUp button.
				b.click();
			}
		}
	}

	Profile.RequestServerValidation_Failed = function(result) {
	}

	Sys.UI.DomEvent.addHandler(window, 'load', Profile.Window_Load);
	
</script>

