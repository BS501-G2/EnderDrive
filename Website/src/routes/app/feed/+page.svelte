<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import { useNavigationContext } from '$lib/client/contexts/navigation'
  import Separator from '$lib/client/ui/separator.svelte'
  import { loremIpsum } from 'lorem-ipsum'
  import { onMount } from 'svelte'
  /*import { MoreVerticalIcon } from 'svelte-feather-icons';
  import { ShareIcon } from 'svelte-feather-icons';
  import { StarIcon } from 'svelte-feather-icons';*/
  import Icon from '$lib/client/ui/icon.svelte'
  const {} = $props()
  const { me } = useServerContext()
</script>

{#await me() then user}
  <div class="page">
    <div class="welcome">
      <h2>
        Welcome,
        {user!.firstName}!
      </h2>
    </div>

    <Separator horizontal />

    <div class="recent"></div>
  </div>
{/await}


	<div class="feed-container">
		<div class="shared-text-container">
			<p class="shared-text-paragraph">Shared Files</p>

			<div class="show-more-button-container">
				<a href="/app/shared">
					<button class="show-more-button">Show more</button>
				</a>
			</div>
		</div>

		<div class="shared-files-container">
			<div class="shared-files-cards-container">

        		<div class="shared-files-cards">
						<div class="thumbnail-cards">
							<img src="/favicon.svg" alt="Thumbnail" />
						</div>
						<div class="name-cards">
							<i class="fa-regular fa-file"></i>
							<p class="card-name-text">resume.docx</p>
						</div>
				</div>

			<div class="shared-files-cards">
					<div class="thumbnail-cards">
						<img src="/favicon.svg" alt="Thumbnail" />
					</div>
					<div class="name-cards">
						<i class="fa-regular fa-file"></i>
						<p class="card-name-text">resume.docx</p>
					</div>
			</div>

			<div class="shared-files-cards">
					<div class="thumbnail-cards">
						<img src="/favicon.svg" alt="Thumbnail" />
					</div>
					<div class="name-cards">
						<i class="fa-regular fa-file"></i>
						<p class="card-name-text">resume.docx</p>
					</div>
			</div>

			<div class="shared-files-cards">
					<div class="thumbnail-cards">
						<img src="/favicon.svg" alt="Thumbnail" />
					</div>
					<div class="name-cards">
						<i class="fa-regular fa-file"></i>
						<p class="card-name-text">resume.docx</p>
					</div>
			</div>

			<div class="shared-files-cards">
					<div class="thumbnail-cards">
						<img src="/favicon.svg" alt="Thumbnail" />
					</div>
					<div class="name-cards">
						<i class="fa-regular fa-file"></i>
						<p class="card-name-text">resume.docx</p>
					</div>
			</div>



		</div>


		<div class="recent-text-container">
			<p class="recent-text-paragraph">Recent Files</p>
		</div>
		<div class="recent-files-container">
			<div class="recent-files-columns">
				<div class="name-column">
					<p class="column-texts">Name</p>
				</div>
				<div class="date-column">
					<p class="column-texts">Size</p>
				</div>
				<div class="size-column">
					<p class="column-texts">Date</p>
				</div>
			</div>

			<div class="recent-files-file-container">
				<div class="file">
					<div class="file-name">
						<p class="file-name-text">resume.docx</p>
					</div>
					<div class="file-size">
						<p class="file-size-text">2 MB</p>
					</div>
					<div class="file-date">
						<p class="file-date-text">2/24/24</p>
					</div>
					<div class="file-icons">
						<div class="fav-icon">
							<i class="fa-solid fa-star"></i>
						</div>
						<div class="share-icon">
							<i class="fa-solid fa-share"></i>
						</div>
						<div class="more-icon">
							<i class="fa-solid fa-ellipsis"></i>
						</div>
					</div>
				</div>




			</div>
		</div>
	</div>
  </div>




<style lang="scss">
  div.page {
    margin: 64px;
    gap: 8px;

    > div.welcome {
      // font-weight: bolder;

      font-size: 2rem;
    }
  }
  .recent-text-container,
	.shared-text-container {
		padding: 20px;
	}
	.shared-text-container {
		display: grid;
		grid-template-columns: 50% 50%;
		align-items: center;
	}
	.show-more-button-container {
		display: flex;
		align-content: flex-end;
		justify-content: flex-end;
		flex-wrap: wrap;

	}
	.show-more-button {
		background-color: var(--primary);
		border: 0px;
		color: white;
		height: auto;
		padding: 8px;
		color: var(--color-1);
		font-size: clamp(8px, 1vw + 0.2rem, 14px);
		transition: background-color .2s;
	}
	.show-more-button:hover{
		background-color: #0000004f;
	}
	.feed-container {
		display: flex;
		flex-direction: column;
		box-sizing: border-box;
		padding: 20px;
		overflow: auto;
	}
	.recent-files-container,
	.shared-files-cards-container,
	.shared-files-container {
		border-radius: 20px;
		background-color: var(--primaryContainerVariant);

	}
  	div.shared-files-cards-container {
   		min-height: 100px;
		min-width: 80px;

  	}

	.shared-files-cards-container {
		display: flex;
		flex-direction: row;
		padding: 16px;
		overflow: hidden;
		height: 190px;
		overflow-x: auto;

	}
	.shared-files-cards {
		display: flex;
		flex-direction: column;
		margin: 8px;
		padding: 4px;
		border-radius: 8px;
		background-color: var(--background);
		color: var(--onBackground);
		border: solid 1px var(--color-1);


	}
	.shared-files-cards:hover{
		background-color: var(--color-5);
	}
	.thumbnail-cards {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		min-width: 100%;
		height: 65%;
		box-sizing: border-box;
		padding: 8px;
		aspect-ratio: 4 / 3;
		overflow: hidden;
	}
	.name-cards {
		display: flex;
		flex-direction: row;
		align-items: center;
		padding: 8px;
		gap: 8px;
		min-height: 1.2em;
		max-width: 100%;
		box-sizing: border-box;
	}

	.recent-files-columns {
		display: grid;
		grid-template-columns: 50% 20% 30%;
		background-color: var(--color-9);
		border-bottom: 1px var(--color-1) solid;
		margin: 5px;

		padding-bottom: 20px;
		position: sticky;
		top: -20px;
		opacity: 100;
	}
	.file-name-text,
	.file-date-text,
	.file-size-text {
		font-size: clamp(10px, 1vw + 0.5rem, 20px);
	}

	.file {
		display: grid;
		grid-template-columns: 50% 20% 20% 10%;
		height: 40px;
		margin: 5px;
		border-bottom: 1px var(--color-1) solid;
		align-items: center;

	}
	.file:hover{
		background-color: var(--color-5);
	}
	.file:hover .fav-icon {
		visibility: visible;
	}
	.file:hover .share-icon {
		visibility: visible;
	}
	.file:hover .more-icon {
		visibility: visible;
	}
	.file-icons {
		display: grid;
		grid-template-columns: 20% 20% 20%;
		align-items: center;
		flex-direction: row;
		justify-content: center;

	}
	.fav-icon,
	.share-icon,
	.more-icon {
		margin-left: 5px;
		margin-right: 5px;
		visibility: hidden;
	}
	.recent-text-paragraph,
	.shared-text-paragraph {
		font-size: clamp(15px, 3vw + 0.5rem, 30px);
	}
	.column-texts {
		font-size: clamp(12px, 3vw + 0.5rem, 24px);
	}

	@media (max-width: 767px) {
		.shared-text-container{
			padding: 0px;
		}

		.show-more-button{
			font-size: clamp(10px, 1.5vw + 0.5rem, 20px);
			margin-right: 16px;
		}
		.shared-files-cards-container{
			display: flex;
			flex-direction: row;
			overflow-x: auto;
			grid-template-columns: repeat(auto-fit, minmax(80px, .2fr));
		}
		.shared-files-cards{
			display: grid;
			grid-template-rows: 65% 35%;
		}
		.thumbnail-cards{
			height: 100%;
			width: 100%;
		}
		.recent-files-container {
			border-radius: 5px;
		}
		.recent-files-columns {
			grid-template-columns: 33% 33% 33%;
			top: 0px;
			padding: 5px;
			display: none;
		}
		.recent-text-container {
			padding: 5px;
		}
		.file {
			grid-template-columns: 50% 30% 20%;
			grid-template-rows: 50% 50%;
			margin: 5px;
		}
		.file-icons {
			display: grid;
			grid-template-columns: auto;
			grid-area: 1 / 3 / 3 / 4;
			align-items: center;
			justify-content: center;
		}
		.fav-icon,
		.share-icon,
		.more-icon {
			visibility: visible;
		}
		.fav-icon,
		.share-icon {
			display: none;
		}

		.file-name-text {
			font-weight: bolder;
			font-size: clamp(12px, 1.5vw + 0.5rem, 24px);
		}
		.feed-container {
			width: 100vw;
			padding: 0px;
			padding-bottom: 5px;
		}
		.file-date {
			grid-column: 1;
			grid-row: 2;
		}
		.file-size {
			grid-column: 2;
			grid-area: 1 / 2 / 3 / 3;
			display: flex;
			justify-content: center;
			align-items: center;
		}
	}
</style>
