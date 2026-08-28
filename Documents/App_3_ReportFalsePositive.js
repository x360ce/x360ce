// Fills Microsoft's file submission form in a browser that is already signed in,
// and stops before sending. Driven by App_3_ReportFalsePositive.ps1, which gathers
// the facts and writes them to the JSON file named as the only argument.
//
// It attaches to a browser started with a debugging port rather than starting one
// of its own, because the form is behind a Microsoft account sign-in. Signing in is
// the one part that has to stay human, and a browser it can attach to is how the
// person and the script share the same window.
//
// Setting the file input directly is also why this exists: clicking "Select" opens
// a chooser owned by the operating system, which a page cannot fill in for you.

const { chromium } = require('playwright');
const fs = require('fs');

const FORM = /filesubmission/i;

// How the fields are named on the page. One place to correct when Microsoft moves
// something, rather than a selector buried in each step.
const FIELD = {
  product: '#comboMsProduct',
  company: '#CompanyName',
  file: 'input[type=file]',
  filePickerBox: '#filePickerBox',
  opinionFalsePositive: '#userOpinionClean',
  detection: '#detectionName',
  definition: '#signatureVersion',
  comments: '#textareaAddComments',
};

async function findPage(browser) {
  const pages = [];
  for (const context of browser.contexts()) pages.push(...context.pages());
  return pages.find((p) => FORM.test(p.url()));
}

// The form only exists once Microsoft knows who you are, so its appearance is the
// signal that signing in finished. Waiting for a field beats waiting for a URL:
// the address is the same before and after.
async function waitForSignIn(browser, minutes) {
  const deadline = Date.now() + minutes * 60000;
  let announced = false;
  while (Date.now() < deadline) {
    const page = await findPage(browser);
    if (page && (await page.locator(FIELD.detection).count()) > 0) return page;
    if (!announced) {
      console.log('  waiting for the sign-in to finish in the browser window...');
      announced = true;
    }
    await new Promise((r) => setTimeout(r, 2000));
  }
  return null;
}

// The product is a combo box rather than a list, so it is opened, read, and the
// entry that names Defender is clicked. Typing into it leaves the page thinking
// nothing was chosen.
async function chooseProduct(page, wanted) {
  const box = page.locator(FIELD.product);
  if ((await box.count()) === 0) return 'no product box on the page';
  await box.click();
  await page.waitForTimeout(500);
  const options = page.locator('[role=option], li[role=option], ul li');
  const count = await options.count();
  for (let i = 0; i < count; i++) {
    const text = ((await options.nth(i).innerText()) || '').trim();
    if (wanted.test(text)) {
      await options.nth(i).click();
      return text;
    }
  }
  await page.keyboard.press('Escape');
  return 'no entry matched, so it was left alone';
}

// The page paints its own control over each radio, so the real input cannot be
// clicked: the span on top swallows the pointer. Click what the person sees, and
// keep the input as the fallback for when that stops being true.
async function chooseRadio(page, selector, labelText) {
  const painted = page.locator('span[role=radio]', { hasText: labelText });
  if ((await painted.count()) > 0) {
    await painted.first().click();
  } else {
    await page.check(selector, { force: true });
  }
  return page.locator(selector).isChecked();
}

(async () => {
  const configPath = process.argv[2];
  if (!configPath || !fs.existsSync(configPath)) {
    console.error('FAILED: no report file was given');
    process.exit(1);
  }
  const report = JSON.parse(fs.readFileSync(configPath, 'utf8'));

  const browser = await chromium.connectOverCDP(report.debuggerUrl || 'http://127.0.0.1:9222');
  const page = await waitForSignIn(browser, report.signInMinutes || 10);
  if (!page) {
    console.error('FAILED: the form did not appear. Sign in, then run this again.');
    await browser.close();
    process.exit(1);
  }
  await page.bringToFront();

  const chosen = await chooseProduct(page, /defender antivirus/i);
  console.log('  product          : ' + chosen);

  if (report.company) {
    await page.fill(FIELD.company, report.company);
    console.log('  company          : ' + report.company);
  }

  // Attaching the file here is the point: the operating system's chooser never
  // opens, so nothing has to be picked by hand.
  if (report.filePath && fs.existsSync(report.filePath)) {
    await page.setInputFiles(FIELD.file, report.filePath);
    await page.waitForTimeout(500);
    const shown = await page.inputValue(FIELD.filePickerBox).catch(() => '');
    console.log('  file             : ' + (shown || report.filePath));
  }

  // What is being disputed. Without this the report reads as somebody agreeing
  // with the detection.
  const marked = await chooseRadio(page, FIELD.opinionFalsePositive,
    'Incorrectly detected as malware');
  console.log('  your opinion     : incorrectly detected as malware' +
    (marked ? '' : '  (NOT SET, choose it by hand)'));

  await page.fill(FIELD.detection, report.detectionName || '');
  console.log('  detection name   : ' + (report.detectionName || '(none given)'));

  if (report.definitionVersion) {
    await page.fill(FIELD.definition, report.definitionVersion);
    console.log('  definition       : ' + report.definitionVersion);
  }

  await page.fill(FIELD.comments, report.comments || '');
  // Nudge the field with real keystrokes afterwards. Filling sets the value and
  // raises an input event, but a page that counts characters on key presses has
  // not noticed, and one keeping its own copy of the field would post an empty
  // comment while the screen shows a full one.
  await page.focus(FIELD.comments);
  await page.keyboard.press('End');
  await page.keyboard.type(' ');
  await page.keyboard.press('Backspace');
  console.log('  explanation      : ' + (report.comments || '').split('\n')[0] + ' ...');

  if (report.screenshot) {
    await page.screenshot({ path: report.screenshot, fullPage: true });
    console.log('  picture of it    : ' + report.screenshot);
  }

  console.log('\n  Nothing was sent. Read it over in the browser and send it yourself.');
  await browser.close();
})().catch((e) => {
  console.error('FAILED: ' + e.message);
  process.exit(1);
});
