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
const isWorking = ref(false);
const isFormValid = ref(false);
const hasError = ref(false);
const errMsg = ref("error message");

// -------------------------------------------------------------------------------------------
function beginWork() {
  isWorking.value = true;
  hasError.value = false;
  errMsg.value = "";

  // All of the states can be updated here....
}

// -------------------------------------------------------------------------------------------
function endWork() {
  isWorking.value = false;
}

// -------------------------------------------------------------------------------------------
async function tryLogin() {
  if (emailAddress != "" && password != "" && !isWorking.value) {
    beginWork();

    await _Login.Login(emailAddress, password).then((res) => {
      if (res.Error) {
        hasError.value = true;
        errMsg.value = "Could not log in at this time.  Please try again later.";
      }
      else {
        const data: LoginResponse = res.Data!;
        if (res.Success && data.IsLoggedIn) {
          alert('update the current login state!');
        }
        else {
          hasError.value = true;
          errMsg.value = data.Message;
          //stateClass.value = "";

          if (data.Code == 0x13) {  // TODO: Define const.
            // Not verified, display the verification message.....
            //stateClass.value = "not-verified";

            errMsg.value = 'This account has not been verified. You should have received an email, or you may <a href="">request another</a>.'

          }
          // else {
          //   hasError.value = true;
          //   errMsg.value = data.Message;
          // }
        }
      }

      endWork();
    });
  }
  else {
    alert('login not available!');
  }
}

// -------------------------------------------------------------------------------------------
function forgotPassword() {
  alert('not implemented!');
}

// -------------------------------------------------------------------------------------------
function validateForm2() {
  alert('i am validate 2');
}
// -------------------------------------------------------------------------------------------
function validateForm() {
//   // Revalidate the form.....
 console.log('email=' + emailAddress);

  const isValid = emailAddress != "" && password != "";
  isFormValid.value = isValid;
}

</script>

<template>
  <h2>Log In</h2>

  <!-- NOTE: Custom events don't bubble in vue3 because the authors are off their meds.
  It seems to me that the easiest way to handle validation is to just catch the input event
  at top level, and then trigger whatever.... -->
  <EZForm :is-working="isWorking" css-classes="login" :error-message="errMsg" @validate="validateForm2" @input="validateForm">
    <EZInput type="email" name="email" v-model="emailAddress" placeholder="Email"  />

    <div class="input">
      <input type="password" name="password" v-model="password" placeholder="Password" :disabled="isWorking"
        @input="validateForm" />
    </div>
    <button type="button" @click="tryLogin" :disabled="!isFormValid || isWorking">Login</button>
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