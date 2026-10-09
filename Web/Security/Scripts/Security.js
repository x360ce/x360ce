var Security = Security ? Security : {}

var Header;

//=========================================================
// Control buttons
//---------------------------------------------------------

Security.Pfx = "UserEditControl1";

Security.UpdateSource = function(username) {
	// If field name was posted then ajust form;
	if (opener != null){
		opener.document.getElementById("SelectUserTextBox").value = username;
		opener.document.getElementById("SelectUserButton").click();
	}
}

Security.CheckCreateStatus = function() {
	var control = document.getElementById(Security.Pfx+"_CreatedUserTextBox");
	var success = (control.value.length > 0);
	if (success) {
		Security.UpdateSource(control.value);
	}
}

Security.CreateButton_Click = function(sender, e) {
	document.getElementById("CreateButtonServer").click();
}

Security.UpdateButton_Click = function(sender, e) {
	document.getElementById("UpdateButtonServer").click();
}

Security.CloseButton_Click = function(sender, e) {
	window.close();
}

Security.InitButtons = function() {
	var cb = document.getElementById("CreateButton");
	if (cb) cb.onclick = Security.CreateButton_Click;
	var ub = document.getElementById("UpdateButton");
	if (ub) ub.onclick = Security.UpdateButton_Click;
	//document.getElementById("CloseButton").onclick = CloseButton_Click;
}		