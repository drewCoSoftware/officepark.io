<script setup lang="ts">


// This looks useful...
// https://stackoverflow.com/questions/73257534/vue-multiple-components-in-a-single-file
import EZForm from '../components/EZForm.vue'
import EZInput from '../components/EZInput.vue'

import { useLoginStore } from '../stores/login';
import type { ILoginState, LoginResponse } from "../stores/login";
import { ref, watch } from 'vue';


const _Login = useLoginStore();

// For each page, or every so often we want to update the login status...
// NOTE: The back-end of the application is responsible for evauluating the
// permissions of the current user.
const loginState = ref(_Login.GetState());

// Form properties.
let emailAddress = "";
let password = "";
//const isWorking = ref(false);
const isFormValid = ref(false);

const form = ref<typeof EZForm>();

// -------------------------------------------------------------------------------------------
// Hmmm.... this is a bit hacky IMO, but we are just trying stuff out I guess....
function isWorking() {
  const f = form.value;
  if (f == null) { return false; }
  return f.isWorking;
}

// -------------------------------------------------------------------------------------------
function beginWork() {
  (form.value as typeof EZForm).beginWork();
  // All of the states can be updated here....
}

// -------------------------------------------------------------------------------------------
function endWork() {
  (form.value as typeof EZForm).endWork();
  //  isWorking.value = false;
}

// -------------------------------------------------------------------------------------------
async function tryLogin() {
  if (emailAddress != "" && password != "" && !isWorking()) {
    beginWork();

    await _Login.Login(emailAddress, password).then((res) => {
      if (res.Error) {
        form.value?.SetErrorMessage("Could not log in at this time.  Please try again later.");
        password = "";
      }
      else {
        const data: LoginResponse = res.Data!;
        if (res.Success && data.IsLoggedIn) {
          alert('update the current login state!');
        }
        else {
          form.value?.SetErrorMessage(data.Message);

          if (data.Code == 0x13) {  // TODO: Define const.
            // Not verified, display the verification message.....
            // If we want to reverify at this time, we should maybe use a one-time cookie....?
            // This will take us to a different page that will fire off the reverification request...?
            form.value?.SetErrorMessage('This account has not been verified. You should have received an email, or you may <a href="/verify?user=' + emailAddress + '">request another</a>.');
            password = "";
          }

        }
      }

      endWork();
    });
  }
  else {
    alert('login not available!');
  }

  validateForm();
}

// -------------------------------------------------------------------------------------------
function forgotPassword() {
  alert('not implemented!');
}

// -------------------------------------------------------------------------------------------
function validateForm() {
  const isValid = emailAddress != "" && password != "";
  isFormValid.value = isValid;
}

</script>

<template>
  <h2>Log In</h2>

  <!-- NOTE: Custom events don't bubble in vue3 because the authors are off their meds.
  It seems to me that the easiest way to handle validation is to just catch the input event
  at top level, and then trigger whatever.... -->
  <EZForm ref="form" css-classes="login">
    <EZInput type="email" name="email" v-model="emailAddress" placeholder="Email" />

    <div class="input">
      <input type="password" name="password" v-model="password" placeholder="Password" :disabled="isWorking()"
        @input="validateForm" />
    </div>
    <button type="button" @click="tryLogin" :disabled="!isFormValid || isWorking()">Login</button>
  </EZForm>

  <div class="actions">
    <p>New User? <a href="/register">Register Here!</a></p>
    <p>Forgot your <a href="/reset">password</a>?</p>
  </div>
</template>

<style scoped lang="less">
.actions {
  font-size: 0.7rem;
  margin-top: 1.0rem;
  text-align: center;

  .forgot {
    display: none;
  }

  .reverify {
    display: none;
  }
}

.actions.not-verified {
  >div.reverify {
    display: block;
  }
}

.actions.login-failed {
  .forgot {
    display: block;
  }
}
</style>