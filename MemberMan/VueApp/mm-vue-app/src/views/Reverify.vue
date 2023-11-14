<script setup lang="ts">

import type EZInputVue from '@/components/EZInput.vue';
import EZForm from '@/components/EZForm.vue';
import { onMounted, ref } from 'vue';
import { watch } from 'fs';

const props = defineProps({
  user: String,
});

class FormProps {
  IsWorking = ref(false);
  IsValid = ref(false);
  HasError = ref(false);
  ErrMsg = ref("");
}
const _FormProps: FormProps = new FormProps();

const isWorking = ref(false);

const form = ref<typeof EZForm>();

// alert('the user is: ' + props.user);

onMounted(async () => {

  // Send the reverification request...
  // alert(form.isWorking);
  // console.log(form.value.test);
  // form.value.test = true;
  // console.log('val is: ' + form.value.test);
  // console.log(form.value?.arguments);
  // form.value.test = true;

  // We can't watch the form values, so we would have to hook up
  // some kind of events instead....  That is an OK solution, of course.
  // watch(form.value.test, (x) => {
  //   alert('a form property was changed!');
  // });
    // form.value.beginWork();
//    form.value.isWorking = true;
});

</script>




<template>
  <p>HI!  I am the reverify page!</p>
  <p>The user is: {{user}}</p>


  <EZForm ref="form">

  </EZForm>

</template>