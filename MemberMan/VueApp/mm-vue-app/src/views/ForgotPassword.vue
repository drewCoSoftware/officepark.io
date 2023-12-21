<script setup lang="ts">

import { onMounted, ref } from 'vue';
import EZForm from '../components/EZForm.vue'
import EZInput from '../components/EZInput.vue'
import { isNullOrEmpty } from '@/shared';
import { useLoginStore } from '@/stores/mmlogin';
import { useRoute, useRouter } from 'vue-router';
import type { IApiResponse } from 'fetchy';
import { inject } from 'vue';


const IsTestMode = inject('isTestMode');

const form = ref<typeof EZForm>();

const INPUT_STATE = "Input";
const SUBMITED_STATE = "Submitted";
const NEW_PASSWORD_STATE = "NewPassword";
const NEW_PASSWORD_EXPIRED_STATE = "NewPasswordExpired";
const NEW_PASSWORD_OK_STATE = "NewPasswordOK";
const INVALID_TOKEN_STATE = "InvalidResetToken";

let _CurState = ref(INPUT_STATE);

const _Login = useLoginStore();
const _Router = useRouter();

let username: string = "";
let resetToken: string = "";
let newPassword: string = "";
let confirmPassword: string = "";

const route = useRoute();

onMounted(() => {
  // Logged in users don't need to remember their passwords.
  if (_Login.IsLoggedIn) {
    _Router.push("/account");
  }

  // querystring handlers...
  resetToken = route.query['resetToken']?.toString() ?? "";
  if (!isNullOrEmpty(resetToken)){
    SetState(NEW_PASSWORD_STATE);
  }
});


// ---------------------------------------------------------------------------------
function validateForm() {
  let isValid = true;
  switch (_CurState.value) {
    case INPUT_STATE:
      isValid = !isNullOrEmpty(username);
      break;

    case NEW_PASSWORD_STATE:
      // TODO: This is a place where we can indicate to the user
      // that the passwords need to match!

      const hasInputs = !isNullOrEmpty(resetToken) &&
        !isNullOrEmpty(newPassword) &&
        !isNullOrEmpty(confirmPassword);

      const passwordsMatch = newPassword == confirmPassword;
      isValid = hasInputs && passwordsMatch;
    default:
      break;
  }

  return isValid;
}

// ---------------------------------------------------------------------------------
async function trySetNewPassword() {
  form.value?.beginWork();

  const response = await _Login.ResetPassword(resetToken, newPassword, confirmPassword);
  if (response.Error) {
    form.value?.SetErrorMessage("Could not reset your password at this time.  Please try again later...");
  }
  else {
    const data: IApiResponse = response.Data!;
    if (data.Code != 0 || response.StatusCode != 200) {
      if (data.Code == 0x16) {
        SetState(NEW_PASSWORD_EXPIRED_STATE);
      }
      else if (data.Code == 0x15) {
        SetState(INVALID_TOKEN_STATE);
      }
      else {
        form.value?.SetErrorMessage(data.Message);
      }
    }
    else {
      form.value?.ClearErrors();
      SetState(NEW_PASSWORD_OK_STATE);
    }
  }

  form.value?.endWork();
}

// ---------------------------------------------------------------------------------
async function trySubmit() {

  (form.value)?.beginWork();

  const response = await _Login.ForgotPassword(username);
  if (response.Error) {
    form.value?.SetErrorMessage("Could not begin the forgot password process at this time.  Please try again later...");
  }
  else {
    const data: IApiResponse = response.Data!;
    if (data.Code != 0 || response.StatusCode != 200) {
      form.value?.SetErrorMessage(data.Message);
    }
    else {
      form.value?.ClearErrors();
      SetState(SUBMITED_STATE);
    }
  }

  (form.value)?.endWork();

}

// --------------------------------------------------
function SetState(state: string) {
  _CurState.value = state;
}

</script>


<template>
  <h2>Forgot Password?</h2>

  <div v-if="_CurState == INPUT_STATE">
    <p>Input your email address and press submit to begin the password reset process.</p>
    <EZForm ref="form" :validate="validateForm">
      <EZInput type="email" name="username" v-model="username" placeholder="username" />
      <button data-is-submit="true" type="button" @click="trySubmit">Submit</button>
    </EZForm>

  </div>

  <div v-if="_CurState == SUBMITED_STATE" class="thank-you">
    <h3>Submitted</h3>
    <p>If your email address is in our system you will receive password reset instructions soon.</p>
    <p>Follow the link in the email to complete the reset process.</p>
  </div>

  <div v-if="_CurState == NEW_PASSWORD_STATE">
    <h3>Set new Password</h3>
    <p>Enter your new password and press submit</p>
    <EZForm ref="form" :validate="validateForm">
      <!-- <EZInput type="email" name="username" v-model="username" placeholder="username" /> -->
      <EZInput type="password" name="newPassword" v-model="newPassword" placeholder="new password" />
      <EZInput type="password" name="confirmPassword" v-model="confirmPassword" placeholder="confirm password" />
      <input type="hidden" name="resetToken" v-model="resetToken" />

      <button data-is-submit="true" type="button" @click="trySetNewPassword">Submit</button>
    </EZForm>
  </div>

  <div v-if="_CurState == NEW_PASSWORD_EXPIRED_STATE">
    <h3>Expired</h3>
    <p>Your password could not be reset at this time because your verification code has expired!</p>
    <p><a href="/forgot-password?username={{username}}">Click here</a> to restart the password reset process:</p>
  </div>

  <div v-if="_CurState == NEW_PASSWORD_OK_STATE">
    <h3>Success!</h3>
    <p>Your new password has been successfully set!</p>
    <p>You may now <a href="/login">Log In</a> with it.</p>
  </div>

  <div v-if="_CurState == INVALID_TOKEN_STATE">
    <h3>Invalid Token</h3>
    <p>The password reset token that you have provided is invalid!</p>
    <p><a href="/forgot-password?username={{username}}">Click here</a> to restart the password reset process:</p>
  </div>

  <div class="test-options" v-if="IsTestMode">
    <h3>TEST OPTIONS</h3>
    <p>Use the buttons below to manually set a form state.</p>
    <button @click="SetState(INPUT_STATE)">Input User</button>
    <button @click="SetState(SUBMITED_STATE)">Submitted</button>
    <button @click="SetState(NEW_PASSWORD_STATE)">New Password</button>
    <button @click="SetState(NEW_PASSWORD_EXPIRED_STATE)">Password Expired</button>
    <button @click="SetState(INVALID_TOKEN_STATE)">Invalid Reset Token</button>
    <button @click="SetState(NEW_PASSWORD_OK_STATE)">Password OK</button>
  </div>
</template>


<style scoped lang="less">
.content {
  text-align: center !important;
}
</style>