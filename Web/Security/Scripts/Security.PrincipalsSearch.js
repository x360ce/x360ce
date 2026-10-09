var Security = Security ? Security : {}

var colIndexCheckBox = 1;

Security.Controls = {};

Security.OpenUserCreateWindow = function(){
	System.Web.UI.Interface.NewWindow("AddUser.aspx","UserCreateWindow","400","420","center","middle");
}

Security.DeleteButton_Click = function(){
	// Detect source id in order to hit proper server button.
	document.getElementById("DeleteButtonServer").click();
}

Security.ListControl = function(prefix, searchType, searchFilter){
	this.Controls;
	this.Prefix;
	this.KeyIndex = 0;
	this.Search = {};
	var me = this;
	//---------------------------------------------------------
	this.CheckBox_Click = function(sender, e){
		var value = sender.parentNode.parentNode.getElementsByTagName("td")[this.KeyIndex].innerHTML;
		var values = this.Controls.ItemsTextBox.value;
		values = System.Text.StringArray.AddValue(values, value, sender.checked);
		this.Controls.ItemsTextBox.value = values;
	}
	//---------------------------------------------------------
	this.OkButton_Click = function(sender, e){
		me.UpdateSource();
		window.close();
	}
	//---------------------------------------------------------
	this.ApplyButton_Click = function(sender, e){
		me.UpdateSource();
	}
	//---------------------------------------------------------
	this.CancelButton_Click = function(sender, e){
		window.close();
	}
	//---------------------------------------------------------
	this.UpdateSource = function(){
		// If field name was posted then ajust form;
		try{
			//Security.Controls.ActionParam1.value = document.getElementById("PrincipalsTextBox").value;
			//Security.Controls.ActionButton.click();
			//me.Controls.AddSelectedButton.click();
			var oDoc = opener.document;
			oDoc.getElementById(me.Prefix+"ItemsTextBox").value = me.Controls.ItemsTextBox.value;
			oDoc.getElementById(me.Prefix+"AddSelectedButton").click();
			//me.Controls.RefreshListButton.click();
		}catch(ex){
			alert("me.Prefix: "+me.Prefix+"; ex.message: "+ex.message);
		}
	}
	//---------------------------------------------------------
	this.DetectKeyIndex = function(){
		var table = this.Controls.ItemsGridView;
		var rows = table.tBodies[0].rows;
		// Make sure that at least one checkbox exists.
		if (rows.length > 1){
			var row = rows[1];
			var cells = row.getElementsByTagName("TD");
			for (var c = 0; c < cells.length; c++){
				// If there is no HTML nodes inside then...
				if (cells[c].innerHTML.indexOf("<") == -1){
					this.KeyIndex = c;
					break;
				}
			}
		}
	}
	//---------------------------------------------------------
	this.UpdatePrincipalList = function(actionType){
		// If table exists then...
		var table = this.Controls.ItemsGridView;
		if (table){
			var values = this.Controls.ItemsTextBox.value;
			// Route thru arrray of rows.
			var rows = table.tBodies[0].rows;
			for (var r = 0; r < rows.length; r++){
				var row = rows[r];
				var cells = row.getElementsByTagName("TD");
				// If we have at least two cells then...
				if (cells.length >= colIndexCheckBox){
					// Get all checkboxes from cell.
					var checkboxes;
					checkboxes = cells[colIndexCheckBox].getElementsByTagName("INPUT");
					if (checkboxes.length > 0){
						// Get checkbox.
						var checkbox = checkboxes[0];
						// Get key value.
						var value = cells[this.KeyIndex].innerHTML;
						switch (actionType){
							case "SelectAll":
								checkbox.checked = true;
								Security.CheckBox_Click(checkbox);
								break;
							case "InvertAll":
								var oldState = checkbox.checked;
								checkbox.checked = !oldState;
								Security.CheckBox_Click(checkbox);
								break;
							case "UnselectAll":
								checkbox.checked = false;
								Security.CheckBox_Click(checkbox);
								break;
							default:
								// Restore checkbox state.
								var isMatch = System.Text.StringArray.IsMatch(values, value);
								checkbox.checked = isMatch;
								// Attach click event.
								checkbox.onclick = function(){ me.CheckBox_Click(this); };
						
						} // switch
					} // if
				} // if
			} // for
		} // if
	}
	//---------------------------------------------------------
	this.GetControl = function(name){
		this.Controls[name] = document.getElementById(this.Prefix+name);
		return this.Controls[name];
	}
	//---------------------------------------------------------
	this.InitializeEvents = function(){
		// If item was found then...
		var url;
		var windowId;
		if (this.Controls.ItemNameLabel){
			// Buttons [Add...] and [Remove] buttons are displayed.
			if (this.Controls.AddButton) this.Controls.AddButton.onclick = function(){ 
				// If items list contains values
				if (me.Controls.ItemsTextBox.value.length == 0){
					url = "PrincipalsSearch.aspx";
					url += "?Get="+me.Search.Type;
					switch(me.Search.Type){
						case "Role":
							url += "&User="+me.Search.Item;
							break;
						case "User":
							url += "&Role="+me.Search.Item;
							break;
						default:
							break;
					}
					url += "&Filter="+me.Search.Filter;
					windowId = me.Search.Filter+me.Search.Type+"SearchWindow";
					System.Web.UI.Interface.NewWindow(url, windowId, "500", "500", "center", "middle");
				}else{
					me.Controls.AddSelectedButton.click();
				}
			}
			if (this.Controls.RemoveButton) this.Controls.RemoveButton.onclick = function(){
				// If items list contains values
				me.Controls.RemoveSelectedButton.click();
			}
		}else{
			// Buttons [New...] and [Delete] buttons are displayed.
			if (this.Controls.NewButton) this.Controls.NewButton.onclick = function(){ 
				url = "Add"+me.Search.Type+".aspx";
				windowId = "AddUserWindow";
				System.Web.UI.Interface.NewWindow(url, windowId, "360", "400", "center", "middle");
			}
			if (this.Controls.DeleteButton) this.Controls.DeleteButton.onclick = function(){ 
				me.Controls.DeleteSelectedButton.click();
			}
		}
		// Init search buttons.
		if (this.Controls.MainPanel){
			if (this.Controls.OkButton) this.Controls.OkButton.onclick = this.OkButton_Click; 
			if (this.Controls.ApplyButton) this.Controls.ApplyButton.onclick = this.ApplyButton_Click; 
			if (this.Controls.CancelButton) this.Controls.CancelButton.onclick = this.CancelButton_Click;
		}
	}
	//---------------------------------------------------------
	this.InitializeClass = function(){
		this.Prefix = prefix;
		this.Controls = {};
		this.GetControl("MainPanel");
		this.GetControl("ItemsGridView");
		// List control.
		this.GetControl("ItemsTextBox");
		this.GetControl("AddButton");
		this.GetControl("RemoveButton");
		this.GetControl("NewButton");
		this.GetControl("DeleteButton");
		// Server side hidden butons.
		this.GetControl("AddSelectedButton");
		this.GetControl("RemoveSelectedButton");
		this.GetControl("DeleteSelectedButton")
		this.GetControl("RefreshListButton");
		// Buttons on update form.
		this.GetControl("CreateButton");
		this.GetControl("UpdateButton");
		// Init search.
		this.Search["Type"] = searchType;
		this.Search["Filter"] = searchFilter;
		this.GetControl("ItemNameLabel");
		if (this.Controls.ItemNameLabel){
			this.Search["Item"] = this.Controls.ItemNameLabel.innerHTML;
		}
		// Init Principals search if available.
		this.Controls["OkButton"] = document.getElementById("OkButton");
		this.Controls["ApplyButton"] = document.getElementById("ApplyButton");
		this.Controls["CancelButton"] = document.getElementById("CancelButton");
		// If control was found then...
		if (this.Controls.ItemsGridView){
			this.DetectKeyIndex();
			this.UpdatePrincipalList();
		}
		this.InitializeEvents();
	}
	this.InitializeClass();
}


//Security.InitializePage = function(){
//	Security.Controls["Table"] = document.getElementById("UserList1_ItemsGridView");
//	var oDoc = opener.document;
//	Security.Controls["ActionParam1"] = oDoc.getElementById("UserListInRole_tbxUserListForAction");
//	Security.Controls["ActionButton"] = oDoc.getElementById("UserListInRole_btnAddUsersToRole");
//}

Security.InitializeLists = function(){
	Security.Controls["UserList1"] = new Security.ListControl("UserList1_", "User", "Available");
	Security.Controls["RoleList1"] = new Security.ListControl("RoleList1_", "Role", "Available");
	var passwordTextBox = document.getElementById("UserEdit1_PasswordTextBox");
	if (passwordTextBox){
		var confirmTextBox = document.getElementById("UserEdit1_PasswordConfirmTextBox");
		var generateButton = document.getElementById("GenerateButton");
		var advancedButton = document.getElementById("AdvancedButton");
		var showPassCheckBox = document.getElementById("UserEdit1_ShowPasswordCheckBox");
		System.Security.Password.Interface.Attach(passwordTextBox, confirmTextBox, generateButton, advancedButton, showPassCheckBox);
	}
}

//=================================================================
// Control Shortcuts.
//-----------------------------------------------------------------			

var shortKeys;

function OnShortKeysAction(sender,e){
	// Run these actions only on keydown
	if (e.EventName == "OnKeyDown"){
		switch(e.KeyName){
			case "CTRL+A":	Security.UpdatePrincipalList("SelectAll"); break;
			case "CTRL+U":	Security.UpdatePrincipalList("UnselectAll"); break;
			case "CTRL+I":	Security.UpdatePrincipalList("InvertAll"); break;
			case "ESCAPE":	CancelButton_Click(); break;
			case "RETURN":	OkButton_Click(); break;
			default:			
		}		
	}
	var enableKey = null;
	// Return key status
	return enableKey;
}

function InitializeShortKeys(){
	shortKeys = new System.Web.UI.ShortKeys.KeysManager();
	shortKeys.PreventKeys(["CTRL+A","CTRL+U","CTRL+I","ESCAPE"]);
	shortKeys.OnShortCutAction = OnShortKeysAction;
}