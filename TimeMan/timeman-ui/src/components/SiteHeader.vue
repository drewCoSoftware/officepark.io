<template>
  <div id="site-header">
    <div>
      <h1>TimeMan</h1>
      <p>officepark.io</p>
    </div>
    <div><a>Login</a></div>
  </div>
</template>

<script>
export default {
  setup() {
    // This is where we could check for initial login status....
    // That means we have to send cookies around, I guess...
    // alert("I am setting up the site header!");
  },
  name: "SiteHeader",
};
</script>

<style lang="less" scoped>

  #site-header{
    background:red;
    display: flex;
    flex-direction: row;

    > div {
      flex-grow:1;
    }
  }

</style>
