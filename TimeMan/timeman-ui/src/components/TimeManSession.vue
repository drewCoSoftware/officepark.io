<template>
  <div class="tm-session">
    <p>Date:</p>
    <p>{{ sess.sessionName }}</p>
  </div>
</template>


<script>
export default {
  name: "TimeManSession",
  // data() {
  //   return {
  //     sessionName: "Active Session",
  //   };
  // },
  props: [ 'sess' ]
};
</script>
