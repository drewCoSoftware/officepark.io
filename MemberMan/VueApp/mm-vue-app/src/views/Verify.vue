<script setup lang="ts">

import type EZInputVue from '@/components/EZInput.vue';
import EZForm from '@/components/EZForm.vue';
import EZInput from '@/components/EZInput.vue';
import { onMounted, ref } from 'vue';
import { watch } from 'fs';
import { fetchy, fetchyPost } from 'fetchy';
import { useRoute } from 'vue-router';
import { inject } from 'vue';
import { useLoginStore } from '../stores/mmlogin';

const _Login = useLoginStore();

const IsTestMode = inject('isTestMode');

let Username: string = "";
let VerificationCode: string = "";

const IsManualCodeRequest = ref(false);

const ENTER_CODE = "EnterCode";
const ENTER_USERNAME = "RequestCode";
const VERIFY_COMPLETE = "VerifyComplete";
const VerifyOK = ref(false);

// get errors set on form?

let AutoVerify = false;
const CurState = ref(ENTER_USERNAME);

// The states are:
// 1. Verify -> enter code + auto verify.
// 2. Request Verification + auto request.
// --> We should go back to verification code entry at that point.

const route = useRoute();

onMounted(async () => {
  Username = route.query['user']?.toString() ?? "";
  VerificationCode = route.query['code']?.toString() ?? ""


  // Clear the querystring, but keep the rest of the bits.
  window.history.pushState(null, "", "/verify");

  if (VerificationCode != "") {
    SetState(ENTER_CODE, false);
  }

  if (CurState.value == ENTER_USERNAME) {
    if (Username != "") {
      // Send out the verification request here (or mock it)...
      ValidateForm();
      await DoVerificationStep();
    }
  }
  else if (CurState.value == ENTER_CODE) {
    if (VerificationCode != "") {
      ValidateForm();
      await DoVerificationStep();
    }
  }

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
    return Username != "";
  }
  else if (CurState.value == ENTER_CODE) {
    return VerificationCode != "";
  }
  else {
    return true;
  }


}


// -------------------------------------------------------------------------------------------
async function DoVerificationStep() {

  if (!ValidateForm() || isWorking()) {
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
        // alert('there was an error!');
        //        console.log(response.Error);
        Form.value?.SetErrorMessage("Could not request verification at this time.  Please try again later.");
      }
      break;

    case ENTER_CODE:
      let codeResponse = await VerifyCode();
      if (codeResponse.Success) {
        if (codeResponse.Data?.Code != 0) {

          Form.value?.SetErrorMessage(codeResponse.Data?.Message);
        }
        else {
          SetState(VERIFY_COMPLETE);
        }
      }
      else {
        Form.value?.SetErrorMessage("Could not verify your account at this time.  Please try again later.");
      }
      break;

    default:
      // Do nothing.
      break;
  }


  Form.value?.endWork();
}

// -------------------------------------------------------------------------------------------
async function VerifyCode() {
  Form.value?.beginWork();

  const res = _Login.VerifyCode(VerificationCode);
  return res;

}

// -------------------------------------------------------------------------------------------
async function RequestVerification() {

  // NOTE: This should go through the loginstore.....
  Form.value?.beginWork();

  // NOTE: This should go through the loginstore.....
  const res = _Login.RequestVerify(Username);
  return res;

}

// -------------------------------------------------------------------------------------------
function SetState(newState: string, clearquerystring: boolean = true) {
  if (CurState.value != newState) {
    CurState.value = newState;

    // Remove ANY querystring values.
    if (clearquerystring) {
      window.history.pushState(null, "", "/verify");
    }
  }
}

// -------------------------------------------------------------------------------------------
function userInput() {
  SetState(ENTER_USERNAME);
}

// -------------------------------------------------------------------------------------------
function codeInput(isManual: boolean = false) {
  IsManualCodeRequest.value = isManual
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
      <p><a @click="codeInput(true)">I already have a code</a></p>
    </div>
    <div v-if="CurState == ENTER_CODE">
      <div v-if="IsManualCodeRequest == false">
        <h4>Request Sent</h4>
        <p>You will receive a verification email if you have an account registered with us.</p>
        <p>You may follow the provided link in the email directly, or you may manually enter it here when prompted.</p>
      </div>
      <div v-else>
        <h4>Verify Your Account</h4>
        <p>Input your verification code below.</p>
      </div>
    </div>

    <EZForm ref="Form" :validate="ValidateForm">
      <div v-if="CurState == ENTER_USERNAME">
        <EZInput type="email" name="email" v-model="Username" placeholder="Email" />
        <button type="button" @click="DoVerificationStep">Request Code</button>
      </div>
      <div v-if="CurState == ENTER_CODE">
        <EZInput type="text" name="code" v-model="VerificationCode" placeholder="Verification Code" />
        <button type="button" @click="DoVerificationStep">Verify Account</button>
      </div>
    </EZForm>
  </div>

  <div v-else class="complete">
    <h4>Verification Status</h4>
    <div v-if="VerifyOK">
      <p>Your account has been successfully verified! You may now <a href="/login">log in!</a></p>
    </div>
    <div v-else>
      <p>The verification code that you entered is incorrect or expired. You may request a new verification code <a
          href="/verify">here</a>.</p>
    </div>
  </div>

  <div class="test-options" v-if="IsTestMode">
    <h3>TEST OPTIONS</h3>
    <p>Use the buttons below to manually set a form state.</p>
    <button @click="userInput">Input User</button>
    <button @click="codeInput(false)">Input Code</button>
    <button @click="verifyOK">Verify OK</button>
    <button @click="verifyFail">Verify Fail</button>
  </div>
</template>


<style lang="less"></style>