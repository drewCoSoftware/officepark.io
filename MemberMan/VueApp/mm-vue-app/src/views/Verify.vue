<script setup lang="ts">

import type EZInputVue from '@/components/EZInput.vue';
import EZForm from '@/components/EZForm.vue';
import EZInput from '@/components/EZInput.vue';
import { onMounted, ref } from 'vue';
import { watch } from 'fs';
import { fetchy, fetchyPost } from '@/fetchy';


const IsTestMode = true;

let Username: string = "";
let VerificationCode: string = "";

let ReverifyOK = ref(false);

const IsFormValid = ref(false);


const ENTER_CODE = "EnterCode";
const ENTER_USERNAME = "RequestCode";
const VERIFY_COMPLETE = "VerifyComplete";
const VerifyOK = ref(false);

// get errors set on form?

let AutoVerify = false;
const CurState = ref(ENTER_USERNAME);


const props = defineProps({
  user: String,
  code: String
});

// The states are:
// 1. Verify -> enter code + auto verify.
// 2. Request Verification + auto request.
// --> We should go back to verification code entry at that point.


onMounted(async () => {
  Username = props.user ?? "";
  VerificationCode = props.code ?? "";


  SetState(ENTER_USERNAME);
  if (VerificationCode != "") {
    SetState(ENTER_CODE);
  }
  if (CurState.value == ENTER_USERNAME) {
    if (Username != "") {
      // Send out the verification request here (or miock it)...
      ValidateForm();
    }
  }
  else if (CurState.value == ENTER_CODE) {

  }

  // await Reverify();
});


const Form = ref<typeof EZForm>();

// -------------------------------------------------------------------------------------------
// Hmmm.... this is a bit hacky IMO, but we are just trying stuff out I guess....
function isWorking() {
  const f = Form.value;
  if (f == null) { return false; }
  return f.isWorking;
}


// -------------------------------------------------------------------------------------------
function ValidateForm() {

  if (CurState.value == ENTER_USERNAME) {
    // TODO: We can also validate proper email format.
    IsFormValid.value = Username != "";
  }
  else if (CurState.value == ENTER_CODE) {
    IsFormValid.value = VerificationCode != "";
  }
  else {
    IsFormValid.value = true;
  }
}


// -------------------------------------------------------------------------------------------
async function DoVerificationStep() {
  if (!IsFormValid.value || isWorking()) {
    return;
  }

  switch (CurState.value) {
    case ENTER_USERNAME:
      let response = await RequestVerification();
      if (response.Success) {
        // We can change the state now....
        SetState(ENTER_CODE);
        ValidateForm();
      }
      else {
        // Print the error message to the form....
        alert('there was an error!');
        Form.value?.SetErrorMessage(response.Error.Message)
      }
      break;

    case ENTER_CODE:
      await VerifyCode();
      break;

    default:
      // Do nothing.
      break;
  }


  Form.value?.endWork();
}

// -------------------------------------------------------------------------------------------
async function VerifyCode() {
  return;
}

// -------------------------------------------------------------------------------------------
async function RequestVerification() {

  Form.value?.beginWork();

  let headers: Headers = new Headers();
  headers.append("Content-Type", "application/json");

  const p = fetchyPost('https://localhost:7138/api/reverify', {
    Username: Username,
    VerificationCode: '123'
  }, headers);

  return p;

}

// -------------------------------------------------------------------------------------------
async function SetState(newState: string) {
  if (CurState.value != newState) {
    ResetForm();
    CurState.value = newState;
  }
}

// -------------------------------------------------------------------------------------------
function ResetForm() {
  Username = "";
  VerificationCode = "";
}

// -------------------------------------------------------------------------------------------
function userInput() {
  SetState(ENTER_USERNAME);
}

// -------------------------------------------------------------------------------------------
function codeInput() {
  SetState(ENTER_CODE);
}

// -------------------------------------------------------------------------------------------
function verifyOK() {
  SetState(VERIFY_COMPLETE);
  VerifyOK.value = true;
}
// -------------------------------------------------------------------------------------------
function verifyFail() {
  SetState(VERIFY_COMPLETE);
  VerifyOK.value = false;
}

</script>


<template>

  <div v-if="CurState != VERIFY_COMPLETE" class="input">
    <div v-if="CurState == ENTER_USERNAME">
      <h4>Verify Your Account</h4>
      <p>Enter your email address and click <span>Request Code</span> below.</p>
    </div>
    <div v-if="CurState == ENTER_CODE">
      <h4>Request Sent</h4>
      <p>You will receive a verification email if you have an account registered with us.</p>
      <p>You may follow the provided link in the email directly, or you may manually enter it here when prompted.</p>
    </div>

    <EZForm ref="Form" @input="ValidateForm">
      <div v-if="CurState == ENTER_USERNAME">
        <EZInput type="email" name="email" v-model="Username" placeholder="Email" />
        <button type="button" @click="DoVerificationStep" :disabled="!IsFormValid || isWorking()">Request Code</button>
      </div>
      <div v-if="CurState == ENTER_CODE">
        <EZInput type="text" name="code" v-model="VerificationCode" placeholder="Verification Code" />
        <button type="button" @click="DoVerificationStep" :disabled="!IsFormValid || isWorking()">Verify Account</button>
      </div>
    </EZForm>
  </div>

  <div v-else class="complete">
    <h4>Verification Status</h4>
    <div v-if="VerifyOK">
      <p>Your account has been successfully verified!  You may now <a href="/login">log in!</a></p>
    </div>
    <div v-else>
      <p>The verification code that you entered is incorrect or expired.  You may request a new verification code <a href="/verify">here</a>.</p>
    </div>
  </div>

  <div class="test-options" v-if="IsTestMode">
      <h3>TEST OPTIONS</h3>
      <p>Use the buttons below to manually set a form state.</p>
      <button @click="userInput">Input User</button>
      <button @click="codeInput">Input Code</button>
      <button @click="verifyOK">Verify OK</button>
      <button @click="verifyFail">Verify Fail</button>
    </div>

</template>


<style lang="less">
.test-options {
  margin-top: 2rem;
}
</style>