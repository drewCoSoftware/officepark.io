<script setup lang="ts">

import type EZInputVue from '@/components/EZInput.vue';
import EZForm from '@/components/EZForm.vue';
import EZInput from '@/components/EZInput.vue';
import { onMounted, ref } from 'vue';
import { watch } from 'fs';
import { fetchy, fetchyPost } from '@/fetchy';

let emailAddress: string = "";
let reverifyOK = ref(false);

const isFormValid = ref(false);

const props = defineProps({
  user: String,
});

// class FormProps {
//   IsWorking = ref(false);
//   IsValid = ref(false);
//   HasError = ref(false);
//   ErrMsg = ref("");
// }
// const _FormProps: FormProps = new FormProps();

const form = ref<typeof EZForm>();

// -------------------------------------------------------------------------------------------
// Hmmm.... this is a bit hacky IMO, but we are just trying stuff out I guess....
function isWorking() {
  const f = form.value;
  if (f == null) { return false; }
  return f.isWorking;
}


// -------------------------------------------------------------------------------------------
function validateForm() {
  const isValid = emailAddress != "";
  isFormValid.value = isValid;
}


// -------------------------------------------------------------------------------------------
async function reverify() {

  if (!isFormValid.value || isWorking()) { 
    return;
  }
  form.value?.beginWork();

  let headers: Headers = new Headers();
  headers.append("Content-Type", "application/json");

  let p = fetchyPost('https://localhost:7138/api/reverify', {
    Username: emailAddress,
    VerificationCode: '123'
  }, headers);
  p.then(x=> {

    if (x.Error) {
      alert('there was an error!');
    }
    else {
      reverifyOK.value = true;
    }

  });


  //  alert('calling the service!');
  // reverifyOK.value = true;

  form.value?.endWork();
}

onMounted(async () => {
  emailAddress = props.user!; // ?? undefined;
  validateForm();

  await reverify();
});

</script>




<template>
  <!-- <p>HI!  I am the reverify page!</p>
  <p>The user is: {{user}}</p> -->

  <div v-if="!reverifyOK" class="input">
    <p>Enter your email address and click <span>Verify Account</span> below.</p>
    <p>You will receive a verification email if you have an account registered with us.</p>

    <EZForm ref="form" @input="validateForm">
      <EZInput type="email" name="email" v-model="emailAddress" placeholder="Email" />

      <button type="button" @click="reverify" :disabled="!isFormValid || isWorking()">Verify Account</button>
    </EZForm>
  </div>
  <div v-else class="complete">
    <p>Thanks, you should receive an e-mail soon.</p>
  </div>
</template>