<script setup lang="ts">

import { ref } from 'vue';
import { useLoginStore } from '../stores/login';
import type { SignupResponse } from '../stores/login';

const _Login = useLoginStore();

// Form properties.
let emailAddr = "";
let password = "";
const isWorking = ref(false);
const isFormValid = ref(false);
const hasError = ref(false);
const errMsg = ref("error message");
const isSignupOK = ref(false);
const verificationSent = ref(false);

const loginSectionClass = ref("login");
const stateClass = ref("signup-page");

// -------------------------------------------------------------------------------------------
function validateForm() {
  // Revalidate the form.....
  const isValid = emailAddr != "" && password != "";
  isFormValid.value = isValid;
}

// -------------------------------------------------------------------------------------------
function refreshState() {
  let stateVal = "signup-page";
  stateVal += isSignupOK.value ? " signup-complete" : "";
  stateVal += verificationSent.value ? " verification-sent" : "";

  // Update state variables based on what is happening on this page....
  let loginVal = "login" + (isWorking.value ? " working" : "");


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
async function tryRegister() {

  const isValid = emailAddr != "" && password != "";
  if (isValid && !isWorking.value) {
    isWorking.value = true;
    refreshState();

    // await _Login.SignUp(emailAddr, emailAddr, password).then((res) => {
    //   if (res.Error) {
    //     // NOTE: We should only get to this code block in cases of network errors....
    //     console.log(res.Error);
    //     hasError.value = true;
    //     errMsg.value = "Could not register at this time.  Please try again later.";
    //   } else {
    //     const data: SignupResponse = res.Data!;

    //     if (res.Success && data?.IsUsernameAvailable) {
    //       hasError.value = false;
    //       errMsg.value = "";

    //       // We want to display some kind of 'Thank you' or other type message....

    //       // alert('Indicate that the signup is OK!');
    //       onSignupComplete();
    //     }
    //     else {
    //       hasError.value = true;
    //       errMsg.value = data?.Message;
    //     }


    //   }
    // });

    isWorking.value = false;
    refreshState();

  }
}

</script>
<template>
  <div :class="stateClass">

    <h2>Register Account</h2>
    <div :class="loginSectionClass">
      <form :class="hasError ? 'has-error' : ''">
        <div class="messages">
          <p>{{ errMsg }}</p>
        </div>
        <div class="input">
          <input type="email" name="email" v-model="emailAddr" placeholder="Email" :disabled="isWorking" />
        </div>
        <div class="input">
          <input type="password" name="password" v-model="password" placeholder="Password" :disabled="isWorking"
            @input="validateForm" />
        </div>
        <button type="button" @click="tryRegister" :disabled="!isFormValid || isWorking">Register</button>
      </form>
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
  > div {
    text-align: center;
  }

  .thank-you {
    display: none;
  }

  .verification-msg {
    display:none;
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
    display:none;
  }
  .verification-msg {
    display:block;
  }
}

</style>
