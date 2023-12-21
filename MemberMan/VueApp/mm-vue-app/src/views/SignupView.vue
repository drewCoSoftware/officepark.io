<script setup lang="ts">

import { onMounted, ref } from 'vue';
import { useLoginStore } from '../stores/mmlogin';
import type { SignupResponse } from '../stores/mmlogin';
import { useRouter } from 'vue-router';

import EZForm from '../components/EZForm.vue'
import EZInput from '../components/EZInput.vue'

const _Login = useLoginStore();
const _Router = useRouter();

// Form properties.
let emailAddr = "";
let password = "";

const isSignupOK = ref(false);
const verificationSent = ref(false);

const loginSectionClass = ref("login");
const stateClass = ref("signup-page");

const form = ref<typeof EZForm>();

onMounted(() => {
  if (_Login.IsLoggedIn) {
    _Router.push("/account");
  }
});

// -------------------------------------------------------------------------------------------
function validateForm(): boolean {
  const isValid = emailAddr != "" && password != "";
  return isValid;
}

// -------------------------------------------------------------------------------------------
// Hmmm.... this is a bit hacky IMO, but we are just trying stuff out I guess....
function isWorking() {
  const f = form.value;
  if (f == null) { return false; }
  return f.isWorking;
}

// -------------------------------------------------------------------------------------------
function refreshState() {
  let stateVal = "signup-page";
  stateVal += isSignupOK.value ? " signup-complete" : "";
  stateVal += verificationSent.value ? " verification-sent" : "";

  // Update state variables based on what is happening on this page....
  let loginVal = "login" + (isWorking() ? " working" : "");


  loginSectionClass.value = loginVal;
  stateClass.value = stateVal;
}

// -------------------------------------------------------------------------------------------
function onSignupComplete() {
  isSignupOK.value = true;
  verificationSent.value = false;
}

// -------------------------------------------------------------------------------------------
async function requestVerification() {

  await _Login.RequestVerification(emailAddr);
  verificationSent.value = true;

  refreshState();
}

// -------------------------------------------------------------------------------------------
async function trySignup() {

  const isValid = emailAddr != "" && password != "";
  if (isValid && !isWorking()) {

    (form.value)?.beginWork();

    await _Login.SignUp(emailAddr, emailAddr, password).then((res) => {
      if (res.Error) {
        // NOTE: We should only get to this code block in cases of network errors....
        console.log(res.Error);
        (form.value)?.SetErrorMessage(res.Error.message);
        // hasError.value = true;
        // errMsg.value = "Could not sign up at this time.  Please try again later.";
      } else {
        const data: SignupResponse = res.Data!;

        if (res.Success && data?.IsUsernameAvailable) {
          (form.value)?.ClearErrors();

          // We want to display some kind of 'Thank you' or other type message....

          onSignupComplete();
        }
        else {
          (form.value)?.SetErrorMessage(data?.Message);
        }


      }
    });

    (form.value)?.endWork();

    refreshState();

  }
}

</script>
<template>
  <div :class="stateClass">

    <h2>Sign Up for Account</h2>
    <div :class="loginSectionClass">
      <EZForm ref="form" :validate="validateForm">
        <EZInput type="email" v-model="emailAddr" placeholder="Email" />
        <EZInput type="password" v-model="password" placeholder="Password" />
        <button data-is-submit="true" type="button" @click="trySignup">Sign Up</button>
      </EZForm>
    </div>

    <div class="thank-you">
      <h2>Success!</h2>
      <p>Thank you for signing up for an account. You should receive a verification email soon.</p>
      <p>If you have not received an email, <button @click="requestVerification" class="link-button">click here</button>
        to send it again.</p>
    </div>

    <div class="verification-msg">
      <h2>Verification Sent</h2>
      <p>A verification email has been sent. It may take a few minutes to arrive in your inbox, so please be patient.</p>
      <p><a href="/">Return to Login</a></p>
    </div>
  </div>
</template>

<style lang="less">
.signup-page {
  >div {
    text-align: center;
  }

  .thank-you {
    display: none;
  }

  .verification-msg {
    display: none;
  }
}

.signup-page.signup-complete {
  .thank-you {
    display: block;
  }

  .login {
    display: none;
  }
}

.signup-page.verification-sent {
  .thank-you {
    display: none;
  }

  .verification-msg {
    display: block;
  }
}
</style>
