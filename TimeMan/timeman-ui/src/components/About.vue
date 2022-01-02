<template>
  <section>
    <h1>About TimeMan!</h1>
    <p>TimeMan is your time keeping super hero!</p>

    <button v-on:click="logout">Force Logout!</button>
  </section>
</template>

<script>
export default {
  name: "About",

  inject: ["logout"],
};
</script>
