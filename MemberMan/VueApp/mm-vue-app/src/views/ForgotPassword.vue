<script setup lang="ts">

import { onMounted, ref } from 'vue';
import EZForm from '../components/EZForm.vue'
import EZInput from '../components/EZInput.vue'
import { isNullOrEmpty } from '@/shared';
import { useLoginStore } from '@/stores/login';
import { useRouter } from 'vue-router';
import type { IApiResponse } from '@/fetchy';

const form = ref<typeof EZForm>();

const INPUT_STATE = "Input";
const SUBMITED_STATE = "Submitted";
const NEW_PASSWORD_STATE = "NewPassword";
const NEW_PASSWORD_EXPIRED_STATE = "NewPasswordExpired";
const NEW_PASSWORD_OK_STATE = "NewPasswordOK";

let _CurState = ref(INPUT_STATE);

const _Login = useLoginStore();
const _Router = useRouter();

let emailAddress:string;

onMounted(() => {
  // Logged in users don't need to remember their passwords.
  if (_Login.IsLoggedIn) {
    _Router.push("/account");
  }
});


// ---------------------------------------------------------------------------------
function validateForm() {
  let isValid = !isNullOrEmpty(emailAddress);
  return isValid;
}

// ---------------------------------------------------------------------------------
async function trySubmit() {
  const url = "/api/forgot-password";

  (form.value)?.beginWork();

  const response = await _Login.ForgotPassword(emailAddress);
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

  // --------------------------------------------------
  function SetState(state:string) {
    _CurState.value = SUBMITED_STATE;
  }

}

</script>


<template>
  <h2>Forgot Password?</h2>
  
  <EZForm v-if="_CurState == INPUT_STATE" ref="form" :validate="validateForm" >
    <EZInput type="email" name="email" v-model="emailAddress" placeholder="email"  />
    <button data-is-submit="true" type="button" @click="trySubmit">Submit</button>
  </EZForm>

  <div v-if="_CurState == SUBMITED_STATE" class="thank-you">
    <p>Submitted</p>
    <p>If your email address is in our system you will receive password reset instructions soon.</p>
  </div>

</template>